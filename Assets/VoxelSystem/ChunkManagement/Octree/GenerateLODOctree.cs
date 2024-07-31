using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

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
    
    public struct AABBSizeComparer : IComparer<AABB>
    {
        public int Compare(AABB x, AABB y)
        {
            return x.Size.y.CompareTo(y.Size.y);
        }
    }

    /// <summary>
    /// 3D axis aligned bounding box with support for fast ray intersection checking.
    /// Optimized for burst compilation.
    /// </summary>
    /// <remarks>Differs from Unity's <see cref="Bounds"/> as this stores the min and max.
    /// Which is faster for overlap and ray intersection checking</remarks>
    public struct AABB
    {
        public int3 Min;
        public int3 Max;
        public int3 Center => (Min + Max) / 2;
        public int3 Size => Max - Min;

        /// <summary>
        /// Construct an AABB
        /// </summary>
        /// <param name="min">Bottom left</param>
        /// <param name="max">Top right</param>
        /// <remarks>Does not check wether max is greater than min for maximum performance.</remarks>
        public AABB(int3 min, int3 max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Returns wether this AABB overlaps with another AABB
        /// </summary>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public bool Overlaps(in AABB other) =>
        //     all(Max >= other.Min) &&
        //     all(other.Max >= Min);
        //
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public bool Contains(int3 point) => all(point >= Min) && all(point <= Max);

        /// <summary>
        /// Returns wether this AABB fully contains another
        /// </summary>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public bool Contains(in AABB other)
        // {
        //     return all(Min <= other.Min) &&
        //            all(Max >= other.Max);
        // }
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public int3 ClosestPoint(int3 point) => clamp(point, Min, Max);

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public float DistanceSquared(int3 point) => distancesq(point, ClosestPoint(point));
        //
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public bool IntersectsRay(in PrecomputedRay ray, out int3 point)
        // {
        //     if (IntersectsRay(ray.origin, ray.invDir, out int tMin))
        //     {
        //         point = ray.origin + ray.dir * tMin;
        //         return true;
        //     }
        //
        //     point = default;
        //     return false;
        // }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public bool IntersectsRay(in PrecomputedRay ray, out int tMin) =>
        //     IntersectsRay(ray.origin, ray.invDir, out tMin);

        /// <summary>
        /// Returns if a ray intersects with this bounding box.
        /// </summary>
        /// <remarks>This method does not handle the case where a component of the ray is on the edge of the box
        /// and may return a false positive in that case. See https://tavianator.com/2011/ray_box.html and https://tavianator.com/2015/ray_box_nan.html</remarks>

        /// <summary>
        /// Returns wether max is greater or equal than min
        /// </summary>
        // public bool IsValid => all(Max >= Min);

        public static explicit operator Bounds(AABB aabb) => new Bounds((float3)aabb.Center, (float3)aabb.Size);

        public static implicit operator AABB(Bounds bounds) =>
            new AABB((int3)(float3)bounds.min, (int3)(float3)bounds.max);

        public static AABB CreateFromCenterAndSize(int3 center, int size)
        {
            return new AABB(center - size / 2, center + size / 2);
        }
    }
}