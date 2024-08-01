using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using UnityS.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using VoxelSystem;
using Material = UnityEngine.Material;

namespace VoxelSystem
{
    public struct RegionComparer : IComparer<Region>
    {
        public int Compare(Region x, Region y)
        {
            return x.LODLevel.Value.CompareTo(y.LODLevel.Value);
        }
    }

// an octree system for generating voxel chunks. doesn't move around the player - see MovingOctreeSystem for that
    public class OctreeChunkSystem : IDisposable
    {
        public Action<Region> OnUnloadChunk;
        public Action<Region> OnNodeLoaded;

        private readonly int _maxLodLevel;
        private readonly int _size;
        private HashSet<Region> _activeNodes = new();

        // Temp, returned by job.
        private NativeHashSet<Region> _returnedNewChunkBounds;

        private float _unloadTimer;
        public readonly int3 Center;

        public OctreeChunkSystem(int3 center, int size, int depth)
        {
            Center = center;
            _size = size;
            _maxLodLevel = depth;
            _returnedNewChunkBounds = new NativeHashSet<Region>(1024, Allocator.Persistent);
        }

        public void GenerateNewNodes(EntityManager entityManager, int3 playerPosition)
        {
            if (_size == 0) throw new ArgumentException("size zero no");
            _returnedNewChunkBounds.Clear();
            var job = new GenerateLODOctree()
            {
                MaxLodLevel = _maxLodLevel,
                size = _size,
                activeNodesIndex = _returnedNewChunkBounds,
                playerPosition = playerPosition,
                octreeCenter = Center
            };

            JobHandle jobHandle = job.Schedule();
            jobHandle.Complete();

            var outNodesArray = _returnedNewChunkBounds.ToNativeArray(Allocator.Temp);

            if (outNodesArray.Length == 0) Debug.LogError("No nodes created by lod gen job");

            // Loop the job results. If it wasn't already in _activeNodes, call the load event.
            for (int i = 0; i < outNodesArray.Length; i++)
            {
                var region = outNodesArray[i];
                if (!_activeNodes.Add(region)) continue;
                OnNodeLoaded(region);
            }

            UnloadNodes();

            outNodesArray.Dispose();
        }

        public void UnloadNodes()
        {
            var toRemove = new List<Region>();
            foreach (var region in _activeNodes)
            {
                if (!_returnedNewChunkBounds.Contains(region))
                {
                    if (_activeNodes.Contains(region))
                    {
                        OnUnloadChunk?.Invoke(region);
                    }

                    toRemove.Add(region);
                }
            }

            foreach (var region in toRemove)
            {
                _activeNodes.Remove(region);
            }
        }

        public void Dispose()
        {
            // just in case
            UnloadNodes();
            _returnedNewChunkBounds.Dispose();
        }
    }
}