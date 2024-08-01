using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using VoxelSystem.Core;

namespace VoxelSystem
{
    [BurstCompile]
    public struct GenerateLODOctree : IJob
    {
        public const int LodBaseDistance = 50;
        public int MaxLodLevel;
        public int size;
        public NativeHashSet<Region> activeNodesIndex;
        public int3 playerPosition;
        public int3 octreeCenter;

        public void Execute()
        {
            CheckClosesNodes(0, octreeCenter, MaxLodLevel, size);
        }

        private void CheckClosesNodes(int index, int3 center, int lodLevel, int nodeSize)
        {
            float lodDistance = LodBaseDistance * math.pow(2, lodLevel); // Double the distance for each LOD level
            // maybe do an isair check here.
            if (lodLevel != 0 && math.distance(playerPosition, center) < lodDistance)
            {
                int delta = nodeSize / 4;

                for (int i = 0; i < 8; i++)
                {
                    CheckClosesNodes(i, GetPosition(i, center, delta), lodLevel - 1,
                        nodeSize / 2);
                }
            }
            else
            {
                var nodeAabb = AABB.CreateFromCenterAndSize(center, nodeSize);
                var region = Region.FromAABB(nodeAabb);
                activeNodesIndex.Add(region);
            }
        }

        private static int3 GetPosition(int index, int3 parentPosition, int delta)
        {
            int3 deltaSign = ChunkMath.DeltaSigns[index];
            return new int3(parentPosition + (deltaSign * delta));
        }
    }

    // good for sorting
    public struct AABBSizeComparer : IComparer<AABB>
    {
        public int Compare(AABB x, AABB y)
        {
            return x.Size.y.CompareTo(y.Size.y);
        }
    }

    public struct AABB
    {
        public int3 Min;
        public int3 Max;
        public int3 Center => (Min + Max) / 2;
        public int3 Size => Max - Min;

        public AABB(int3 min, int3 max)
        {
            this.Min = min;
            this.Max = max;
        }

        public static explicit operator Bounds(AABB aabb) => new Bounds((float3)aabb.Center, (float3)aabb.Size);

        public static implicit operator AABB(Bounds bounds) =>
            new AABB((int3)(float3)bounds.min, (int3)(float3)bounds.max);

        public static AABB CreateFromCenterAndSize(int3 center, int size)
        {
            return new AABB(center - size / 2, center + size / 2);
        }
    }
}