using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FishNet;
using FishNet.Object;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityS.Transforms;
using VoxelSystem.Colors;
using Random = Unity.Mathematics.Random;

namespace VoxelSystem
{
    public enum RegionInformation
    {
        Air,
        Unmodified,
        Modified
    }

    public struct Chunk : IPoolable, IDisposable
    {
        public RegionInformation AreaInformation;

        public Region Region => Region.FromAABB(Aabb);
        public Entity Entity;

        public const float LOD0VoxelSize = 0.1f;
        public const int ChunkSize = 32;
        public const int ChunkSizeSquared = ChunkSize * ChunkSize;
        public const int ChunkSizeCubed = ChunkSizeSquared * ChunkSize;

        public AABB Aabb;
        private NativeArray<ushort> _colors;
        private NativeArray<float> _noiseVolume0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetColor(int index) => _colors[index];

        public int GetWorldY(int y)
        {
            // voxelsize might as well be multiplier, can be gotten from dividing size by 32
            return (Aabb.Min.y / Aabb.GetSizeMultiplier()) + (y) * Aabb.GetSizeMultiplier();
        }

        public int3 GetWorldPosition(int3 localPosition)
        {
            return Aabb.Min + localPosition * Aabb.GetSizeMultiplier();
        }

        [BurstDiscard]
        public static Chunk Create(AABB aabb)
        {
            var chunk = ConfigureableObjectPool<Chunk>.Instance.GetObject();
            chunk.Aabb = aabb;
            return chunk;
        }

        public Chunk(Allocator allocator)
        {
            _colors = new NativeArray<ushort>(Chunk.ChunkSizeCubed, allocator);
            _noiseVolume0 = new NativeArray<float>(Chunk.ChunkSizeCubed, allocator);
            AreaInformation = RegionInformation.Unmodified;
            Aabb = new AABB();
            Entity = Entity.Null;
        }


        public void SetColorSafe(int x, int y, int z, Color32 color)
        {
            if (x >= 32 || y >= 32 || z >= 32) return;
            if (x < 0 || y < 0 || z < 0) return;
            SetColor(x, y, z, color);
        }

        public void SetColor(int index, Color32 color)
        {
            _colors[index] = ColorUtilities.ConvertColor32To16Bit(color);
        }

        public void SetColor(int3 position, Color32 color) => SetColor(position.x, position.y, position.z, color);
        public void SetColor(int x, int y, int z, Color32 color) => SetColor(ChunkMath.FlattenIndex(x, y, z), color);

        public void SetColorSphere(int3 center, int radius, params Color32[] colors)
        {
            // DOES NOT account for LOD
            // make a sphere with rand between colors - COOL!
            var min = center - radius;
            var max = center + radius;

            for (var x = min.x; x <= max.x; x++)
            for (var y = min.y; y <= max.y; y++)
            for (var z = min.z; z <= max.z; z++)
            {
                var currentPos = new int3(x, y, z);

                if (math.distance(center, currentPos) > radius) continue;

                // rand has to go off voxel pos to be deterministic
                var rand = Unity.Mathematics.Random.CreateFromIndex(math.hash(currentPos));
                int randomIndex = rand.NextInt(0, colors.Length);
                SetColor(currentPos, colors[randomIndex]);
            }

            throw new NotImplementedException();
        }

        public unsafe void Clear()
        {
            UnsafeUtility.MemClear(_colors.GetUnsafePtr(), _colors.Length);
        }

        public NativeArray<ushort>.ReadOnly GetColors()
        {
            return _colors.AsReadOnly();
        }

        public void SetColorVolume(int3 min, int3 max, Color red)
        {
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    for (int z = min.z; z < max.z; z++)
                    {
                        SetColor(x, y, z, red);
                    }
                }
            }
        }

        public int3 GetVoxelPositionClosestToPoint(float3 point)
        {
            // Assuming TargetBounds is accessible within this method and has the required information
            float3 chunkOrigin = Aabb.Min; // World position of the chunk's origin
            float3 chunkSize = Aabb.Size; // Size of the chunk in world units

            // Calculate the local position in the chunk by offsetting the point by the chunk's origin
            // and then scaling according to the size of the chunk.
            // This assumes that the voxel size is the chunk size divided by the number of voxels along each axis (32).
            float3 localPosition = (point - chunkOrigin) / (chunkSize / 32.0f);

            // Since we want the closest voxel, we round to the nearest whole number.
            int3 voxelPosition = new int3(
                Mathf.RoundToInt(localPosition.x),
                Mathf.RoundToInt(localPosition.y),
                Mathf.RoundToInt(localPosition.z)
            );

            // Ensure the local voxel position is clamped within the bounds of the chunk (0 to 31)
            voxelPosition = math.clamp(voxelPosition, new int3(0, 0, 0), new int3(31, 31, 31));

            return voxelPosition;
        }

        // Handy for some stuff
        public int GetClosestLevelWithinChunkToYPos(int worldPosY)
        {
            // Assuming TargetBounds is accessible within this method and has the required information
            int chunkOriginY = Aabb.Min.y; // World Y position of the chunk's origin
            // Calculate the local Y position in the chunk by offsetting the worldPosY by the chunk's origin Y
            // and then scaling according to the size of the chunk along the Y axis.
            // This assumes that the voxel size is the chunk sizeY divided by the number of voxels along the Y axis (32).
            int localPosY = (worldPosY - Aabb.Min.y) / Aabb.GetSizeMultiplier();
            // Clamp the local Y position to the range of [0, 31] to ensure it's within the chunk bounds
            int voxelYPosition = Mathf.Clamp(Mathf.RoundToInt(localPosY), 0, 31);
            return voxelYPosition;
        }

        public void GenerateChunkEdgeOutlines(Color color)
        {
            for (int i = 0; i < Chunk.ChunkSize; i++)
            {
                SetColor(i, 0, 0, color); // Front bottom edge
                SetColor(i, 0, 31, color); // Back bottom edge
                SetColor(0, 0, i, color); // Left bottom edge
                SetColor(31, 0, i, color); // Right bottom edge
                SetColor(i, 31, 0, color); // Front top edge
                SetColor(i, 31, 31, color); // Back top edge
                SetColor(0, 31, i, color); // Left top edge
                SetColor(31, 31, i, color); // Right top edge
                SetColor(0, i, 0, color); // Front left vertical edge
                SetColor(31, i, 0, color); // Front right vertical edge
                SetColor(0, i, 31, color); // Back left vertical edge
                SetColor(31, i, 31, color); // Back right vertical edge
            }
        }

        public void ColorVoxelsBelowSurface(FixedString512Bytes noiseTree, Color32 color, float surface,
            float frequency = 0.01f,
            int seed = 123)
        {
            NativeArray<float>.ReadOnly noiseVolume = GetNoiseVolume(noiseTree, frequency, seed);

            for (int i = 0; i < noiseVolume.Length; i++)
            {
                // If the noise value is below the surface level, set the voxel color to red
                if (noiseVolume[i] < surface)
                {
                    SetColor(i, color);
                }
            }
        }

        public void ColorVoxelsBelowSurface(FixedString512Bytes noiseTree, float surface, float frequency, int seed,
            params Color32[] colors)
        {
            NativeArray<float>.ReadOnly noiseVolume = GetNoiseVolume(noiseTree, frequency, seed);

            for (int i = 0; i < noiseVolume.Length; i++)
            {
                // If the noise value is below the surface level, set the voxel color to red
                if (noiseVolume[i] < surface)
                {
                    var rand = new Random(math.hash(new int4((int3)(float3)Aabb.Min, i)));
                    SetColor(i, colors[rand.NextInt(0, colors.Length)]);
                }
            }
        }

        public unsafe NativeArray<float>.ReadOnly GetNoiseVolume(FixedString512Bytes tree, float frequency = 0.01f,
            int seed = 123)
        {
            int multiplier = Aabb.GetSizeMultiplier();
            UnsafeUtility.MemClear(_noiseVolume0.GetUnsafePtr(), _noiseVolume0.Length * sizeof(float));

            float* minMax = stackalloc float[2];
            var nodeHandle = FastNoise.fnNewFromEncodedNodeTree(tree.GetUnsafePtr());
            if (nodeHandle == IntPtr.Zero) throw new ArgumentException(nameof(tree));

            int3 origin = Aabb.Min / multiplier;

            // This is just an example; you'll need to find the right scaling factor
            float adjustedFrequency = frequency * multiplier;

            FastNoise.fnGenUniformGrid3D(nodeHandle, (float*)_noiseVolume0.GetUnsafePtr(),
                origin.x,
                origin.y,
                origin.z,
                Chunk.ChunkSize,
                Chunk.ChunkSize,
                Chunk.ChunkSize,
                adjustedFrequency, seed, minMax);
            FastNoise.fnDeleteNodeRef(nodeHandle);
            return _noiseVolume0.AsReadOnly();
        }

        // public NativeArray<Point> GetPointOverlaps(Allocator allocator, float spacing, float radius)
        // {
        //     throw new NotImplementedException();
        //     // Calculate the expanded bounds to account for the radius of points
        //     Bounds expandedBounds = new Bounds(Aabb.Center,
        //         Aabb.Size + new Vector3(radius * 2, radius * 2, radius * 2));
        //
        //     // Determine the grid size based on the expanded bounds and point spacing
        //     int3 gridSize = new int3(
        //         Mathf.CeilToInt(expandedBounds.size.x / spacing),
        //         Mathf.CeilToInt(expandedBounds.size.y / spacing),
        //         Mathf.CeilToInt(expandedBounds.size.z / spacing)
        //     );
        //
        //     // Calculate total number of points (including potential overlaps)
        //     int totalPoints = gridSize.x * gridSize.y * gridSize.z;
        //     NativeArray<Point> points = new NativeArray<Point>(totalPoints, allocator);
        //
        //     // Generate the points on the grid
        //     int index = 0;
        //     for (int x = 0; x < gridSize.x; x++)
        //     {
        //         for (int y = 0; y < gridSize.y; y++)
        //         {
        //             for (int z = 0; z < gridSize.z; z++)
        //             {
        //                 // Calculate the point position
        //                 float3 pointPosition = new float3(
        //                     expandedBounds.min.x + x * spacing,
        //                     expandedBounds.min.y + y * spacing,
        //                     expandedBounds.min.z + z * spacing
        //                 );
        //
        //                 Point point = new Point(pointPosition, radius);
        //
        //                 // Check if the point's sphere intersects the original bounds
        //                 if (Aabb.IntersectsSphere(point.Position, point.Radius))
        //                 {
        //                     points[index++] = point;
        //                 }
        //             }
        //         }
        //     }
        //
        //     // Resize the NativeArray to the actual number of overlapping points
        //     if (index < totalPoints)
        //     {
        //         NativeArray<Point> resizedPoints = new NativeArray<Point>(index, allocator);
        //         NativeArray<Point>.Copy(points, resizedPoints, index);
        //         points.Dispose(); // Dispose of the original array
        //         return resizedPoints;
        //     }
        //
        //     return points;
        // }

        [BurstDiscard]
        public void Dispose()
        {
            ConfigureableObjectPool<Chunk>.Instance.ReturnObject(this);
        }

        public void OnCreate()
        {
            Aabb = new Bounds();
            Allocate(Allocator.Domain);
        }

        public void Allocate(Allocator allocator)
        {
            _colors = new NativeArray<ushort>(Chunk.ChunkSizeCubed, allocator);

            CreateNoiseVolumes();
        }

        public void OnRetrievedFromPool()
        {
            throw new NotImplementedException();
        }

        public unsafe void OnReturnedToPool()
        {
            UnsafeUtility.MemClear(_colors.GetUnsafePtr(), _colors.Length * sizeof(ushort));
            UnsafeUtility.MemClear(_noiseVolume0.GetUnsafePtr(), _noiseVolume0.Length * sizeof(float));
        }

        public float3 GetScaledPosition()
        {
            return (float3)Aabb.Min * Chunk.LOD0VoxelSize;
        }

        public float GetScale() => (float)Aabb.GetSizeMultiplier() * Chunk.LOD0VoxelSize;

        public float4x4 GetTRS()
        {
            return float4x4.TRS(GetScaledPosition(), quaternion.identity, GetScale());
        }

        public void SetColors(ushort[] colors)
        {
            if (!_colors.IsCreated)
            {
                _colors = new NativeArray<ushort>(Chunk.ChunkSizeCubed, Allocator.Domain);
            }

            _colors.CopyFrom(colors);
        }

        public void CreateNoiseVolumes(Allocator allocator = Allocator.Domain)
        {
            _noiseVolume0 = new NativeArray<float>(Chunk.ChunkSizeCubed, allocator);
        }
    }
}