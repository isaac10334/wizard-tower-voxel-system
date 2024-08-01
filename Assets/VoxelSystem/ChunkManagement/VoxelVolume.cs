using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VoxelSystem;
using VoxelSystem.Chunks;
using FishNet.Object;
using Unity.Collections;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using UnityS.Physics;
using VoxelSystem.Core;
using VoxelSystem.Meshing;
using Material = UnityEngine.Material;

// A voxel volume - contains it's own octree
// This is on the player prefab!!
public class VoxelVolume : VoxelBehavior
{
    // TODO allow for separate horizontal and vertical sizes.
    // Only specify bounds and system automatically chooses nodes per axis
    private const int OctreesPerAxis = 4;

    // The bounds of one axis of the entire loaded world in voxels. Will be rounded up to a power of 2.
    private int _loadedWorldBoundsSize = 100000;
    private int _lodLevels;
    private int3 _lastCenter = new int3(Int32.MaxValue);
    private float _unloadTimer;
    private int _individualOctreeSize;
    private int3 _lastPlayerPosition = new int3(Int32.MinValue);
    private int _updateDistance = 50;

    private readonly Dictionary<int3, OctreeChunkSystem> _octrees = new();

    private void OnDrawGizmos()
    {
        foreach (var o in _octrees.Values)
        {
            foreach (var r in o.activeNodes)
            {
                var aabb = r.ToAABB();
                Gizmos.DrawWireCube(aabb.Center.ToWorldPos(), aabb.Size.ToWorldPos());
            }
        }
    }

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
        Debug.Log("setting up voxel volume");
        _loadedWorldBoundsSize = math.ceilpow2(_loadedWorldBoundsSize);
        _individualOctreeSize = _loadedWorldBoundsSize / OctreesPerAxis;
        _lodLevels = (int)math.log2((int)(_individualOctreeSize / 32));
        // Debug.Log($"individual octree size: {_individualOctreeSize}. that's {_lodLevels} levels");
    }

    private void Update()
    {
        if (!base.IsOwner)
        {
            return;
        }

        var playerWorldPosition = (float3)transform.position;
        var playerVoxelPosition = (int3)(playerWorldPosition / Chunk.LOD0VoxelSize);
        var center = ChunkMath.SnapToGridCenter((int3)playerVoxelPosition, _individualOctreeSize);

        CreateOctrees(center);
        UpdateOctrees(center, playerVoxelPosition);
    }

    private void UpdateOctrees(int3 center, int3 playerPosition)
    {
        UnloadChunksWithTimer();
        bool playerMovedEnough = math.distance(_lastPlayerPosition, playerPosition) > _updateDistance;

        // Iterate over octrees and unload far away ones, and update others
        var octreesSafe = new List<KeyValuePair<int3, OctreeChunkSystem>>(_octrees);
        foreach (var (position, octree) in octreesSafe)
        {
            if (ShouldOctreeBeUnloaded(center, position))
            {
                octree.Dispose();
                _octrees.Remove(position);
                continue;
            }

            if (playerMovedEnough)
            {
                _lastPlayerPosition = playerPosition;
                octree.GenerateNewNodes(World.DefaultGameObjectInjectionWorld.EntityManager, playerPosition);
            }

            // octree.CompleteQueuedChunks(World.DefaultGameObjectInjectionWorld.EntityManager);
        }
    }

    private void CreateOctrees(int3 center)
    {
        // moved a whole octree worth of distance
        if (center.Equals(_lastCenter)) return;
        _lastCenter = center;

        var halfRd = _loadedWorldBoundsSize / 2;
        var min = center - halfRd;
        var max = center + halfRd;

        for (var x = min.x; x < max.x; x += _individualOctreeSize)
        for (var y = min.y; y < max.y; y += _individualOctreeSize)
        for (var z = min.z; z < max.z; z += _individualOctreeSize)
        {
            var octreeOrigin = new int3(x, y, z);
            var octreeCenter = octreeOrigin + (_individualOctreeSize / 2);
            if (_octrees.ContainsKey(octreeOrigin)) continue;
            var octree = new OctreeChunkSystem(octreeCenter, _individualOctreeSize, _lodLevels);

            octree.OnUnloadChunk += OnUnloadChunk;
            octree.OnNodeLoaded += OnNodeLoaded;

            _octrees.Add(octreeOrigin, octree);
        }
    }

    private HashSet<Region> _existingRegions = new();
    private Dictionary<Region, Chunk> _chunks = new();

    void OnNodeLoaded(Region region)
    {
        // TODO:
        // add to a high-resolution job completion checking queue.
        // the job that is running is data gen, meshing.
        // but first data retrieval.
        // all we do is submit the RPC here.
        // Do NOT Add if it already exists!!
        if (_existingRegions.Add(region))
        {
            RpcGetChunk(region);
        }
    }

    [Client]
    protected override async void OnReceiveChunk(Chunk chunk)
    {
        if (!_existingRegions.Contains(chunk.Region))
        {
            ConfigureableObjectPool<Chunk>.Instance.ReturnObject(chunk);
            return;
        }

        if (chunk.AreaInformation == RegionInformation.Air) return;
        if (chunk.Aabb.GetSizeMultiplier() == 0)
        {
            throw new NotImplementedException(
                $"aab here has size multiplier of zero: {chunk.Aabb.Size}, {chunk.Aabb.Min}, {chunk.Aabb.Max}, {chunk.Aabb.GetSizeMultiplier()}");
        }

        Mesh mesh = null;

        if (chunk.AreaInformation == RegionInformation.Modified)
        {
            var mesher = CubicGreedyMeshCreator.Create(chunk);
            mesh = await mesher.ScheduleFancyJob<Mesh, CubicGreedyMeshCreator>();
        }

        else if (chunk.AreaInformation == RegionInformation.Unmodified)
        {
            chunk.Allocate(Allocator.Domain);
            var generator = VoxelManager.DataGenerator.Create(chunk);
            var job = generator.Schedule();
            var mesher = CubicGreedyMeshCreator.Create(chunk);
            mesh = await mesher.ScheduleFancyJob<Mesh, CubicGreedyMeshCreator>(job);
        }

        if (mesh == null)
        {
            Debug.LogWarning("no mesh!");
            return;
        }

        if (!_existingRegions.Contains(chunk.Region))
        {
            ConfigureableObjectPool<Chunk>.Instance.ReturnObject(chunk);
            return;
        }

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // TODO maybe add chunk to entity directly, maybe not.
        chunk.Entity = entityManager.CreateEntity();
        entityManager.AddComponent<RenderMesh>(chunk.Entity);
        entityManager.AddComponent<RenderBounds>(chunk.Entity);
        var trs = chunk.GetTRS();
        entityManager.AddComponentData(chunk.Entity, new LocalToWorld
        {
            Value = trs
        });
        // TODO - this might offset it twice
        entityManager.AddComponentData(chunk.Entity, new LocalTransform()
        {
            Position = chunk.GetScaledPosition(),
            Rotation = Quaternion.identity,
            Scale = chunk.GetScale()
        });
        entityManager.AddComponent<PhysicsCollider>(chunk.Entity);

        var position = chunk.Aabb.Min;
        var material = VoxelSystemResources.Instance.settingsStore.Settings.DefaultMaterial;

        var renderMeshArray = new RenderMeshArray(
            new Material[] { material },
            new Mesh[] { mesh });

        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.On,
            receiveShadows: true);

        RenderMeshUtility.AddComponents(
            chunk.Entity,
            entityManager,
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

        _chunks.Add(chunk.Region, chunk);
    }

    private async void OnUnloadChunk(Region region)
    {
        if (!Application.isPlaying) return;

        // This line is a shitty system to avoid gaps between chunks
        await Task.Delay(TimeSpan.FromSeconds(1.5f));

        if (!Application.isPlaying) return;

        if (_existingRegions.Remove(region))
        {
            if (_chunks.TryGetValue(region, out Chunk chunk))
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(chunk.Entity);
                ConfigureableObjectPool<Chunk>.Instance.ReturnObject(chunk);
                _chunks.Remove(region);
            }
        }
    }

    private void UnloadChunksWithTimer()
    {
        return;
        // _unloadTimer += UnityEngine.Time.deltaTime;
        // if (_unloadTimer >= 1f)
        // {
        //     _unloadTimer = 0f;
        // }
        foreach (var octree in _octrees.Values)
        {
            octree.UnloadNodes();
        }
    }

    private bool ShouldOctreeBeUnloaded(int3 playerGridCenter, int3 position)
    {
        var halfRd = _loadedWorldBoundsSize / 2;

        var min = playerGridCenter - halfRd;
        var max = playerGridCenter + halfRd;

        var outOfRangeX = position.x < min.x || position.x >= max.x;
        var outOfRangeY = position.y < min.y || position.y >= max.y;
        var outOfRangeZ = position.z < min.z || position.z >= max.z;

        return outOfRangeX || outOfRangeY || outOfRangeZ;
    }

    private void OnDestroy()
    {
        foreach (var octree in _octrees.Values)
        {
            octree.Dispose();
        }
    }
}