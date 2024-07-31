using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using System;

namespace VoxelSystem
{
    public enum Face
    {
        XNegative = 0,
        YNegative = 1,
        ZNegative = 2,
        XPositive = 3,
        YPositive = 4,
        ZPositive = 5
    }

    public static class ChunkMath
    {
        public static readonly int3[] VoxelBasePosition = new int3[8]
        {
            new int3(0, 0, 0),
            new int3(0, 0, 1),
            new int3(1, 0, 0),
            new int3(1, 0, 1),
            new int3(0, 1, 0),
            new int3(0, 1, 1),
            new int3(1, 1, 0),
            new int3(1, 1, 1)
        };

        public static readonly int3[] DeltaSigns = new int3[8]
        {
            new int3(-1, -1, -1),
            new int3(-1, -1, 1),
            new int3(1, -1, -1),
            new int3(1, -1, 1),
            new int3(-1, 1, -1),
            new int3(-1, 1, 1),
            new int3(1, 1, -1),
            new int3(1, 1, 1)
        };

        public static readonly int3 XN = new int3(-1, 0, 0);
        public static readonly int3 YN = new int3(0, -1, 0);
        public static readonly int3 ZN = new int3(0, 0, -1);
        public static readonly int3 XP = new int3(1, 0, 0);
        public static readonly int3 YP = new int3(0, 1, 0);
        public static readonly int3 ZP = new int3(0, 0, 1);
        private static readonly float3 voxelCenterPosition = new float3(0.5f, 0.5f, 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DoCubesIntersect(int4 cube1, int4 cube2)
        {
            return (cube1.x < cube2.x + cube2.w && cube1.x + cube1.w > cube2.x) &&
                   (cube1.y < cube2.y + cube2.w && cube1.y + cube1.w > cube2.y) &&
                   (cube1.z < cube2.z + cube2.w && cube1.z + cube1.w > cube2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideCube(in int3 point, in int4 cube)
        {
            return (point.x >= cube.x && point.x < cube.x + cube.w) &&
                   (point.y >= cube.y && point.y < cube.y + cube.w) &&
                   (point.z >= cube.z && point.z < cube.z + cube.w);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DoesCircleIntersectSquare(in int2 circleCenter, in int radius, in int2 squareCenter,
            in int length)
        {
            int halfLength = length / 2;
            int2 squareMin = squareCenter - new int2(halfLength, halfLength);
            int2 squareMax = squareCenter + new int2(halfLength, halfLength);

            int2 closestPoint = new int2(
                math.clamp(circleCenter.x, squareMin.x, squareMax.x),
                math.clamp(circleCenter.y, squareMin.y, squareMax.y));

            int2 diff = (circleCenter - closestPoint);
            int distanceSquared = diff.x * diff.x + diff.y * diff.y;
            return distanceSquared < radius * radius;
        }

        // Checks if circle with given center touches or overlaps the square.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCircleTouchingOrOverlappingSquare(float2 circleCenter, int squareSize, float radius,
            float2 squareCenter)
        {
            float halfSquareSize = squareSize / 2.0f;
            float2 squareMin = new float2(squareCenter.x - halfSquareSize, squareCenter.y - halfSquareSize);
            float2 squareMax = new float2(squareCenter.x + halfSquareSize, squareCenter.y + halfSquareSize);

            float2 closestPointInSquare = new float2(
                System.Math.Max(squareMin.x, System.Math.Min(squareMax.x, circleCenter.x)),
                System.Math.Max(squareMin.y, System.Math.Min(squareMax.y, circleCenter.y))
            );

            float distanceSquared =
                (circleCenter.x - closestPointInSquare.x) * (circleCenter.x - closestPointInSquare.x) +
                (circleCenter.y - closestPointInSquare.y) * (circleCenter.y - closestPointInSquare.y);

            return distanceSquared <= radius * radius;
        }

        public static double FindRadiusOfCircleContainingSquare(int squarelength)
        {
            double diagonal = System.Math.Sqrt(2) * squarelength;
            double radius = diagonal / 2;
            return radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 SnapToGridWithOffset(int3 dataPos, int gridSize)
        {
            int x = dataPos.x - mod(dataPos.x, gridSize);
            int y = dataPos.y - mod(dataPos.y, gridSize);
            int z = dataPos.z - mod(dataPos.z, gridSize);

            return new int3(x, y, z) + gridSize / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 SnapToGridNoHalf(int3 dataPos, int gridSize)
        {
            int x = dataPos.x - mod(dataPos.x, gridSize);
            int y = dataPos.y - mod(dataPos.y, gridSize);
            int z = dataPos.z - mod(dataPos.z, gridSize);

            return new int3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 FloorToGrid(int3 dataPos, int gridSize)
        {
            int x = dataPos.x - mod(dataPos.x, gridSize);
            int y = dataPos.y - mod(dataPos.y, gridSize);
            int z = dataPos.z - mod(dataPos.z, gridSize);
            return new int3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 SnapToGridCenter(int3 dataPos, int gridSize)
        {
            int x = dataPos.x - mod(dataPos.x, gridSize);
            int y = dataPos.y - mod(dataPos.y, gridSize);
            int z = dataPos.z - mod(dataPos.z, gridSize);
            return new int3(x, y, z) + gridSize / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 FloorToGrid(int3 dataPos, int gridSize, out int3 remainder)
        {
            int x = dataPos.x - mod(dataPos.x, gridSize);
            int y = dataPos.y - mod(dataPos.y, gridSize);
            int z = dataPos.z - mod(dataPos.z, gridSize);

            int3 pos = new int3(x, y, z);
            remainder = dataPos - pos;
            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 CeilToGrid(int3 dataPos, int gridSize)
        {
            int x = dataPos.x + mod(dataPos.x, gridSize);
            int y = dataPos.y + mod(dataPos.y, gridSize);
            int z = dataPos.z + mod(dataPos.z, gridSize);

            return new int3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 RoundToGrid(int3 dataPos, int gridSize)
        {
            int x = (int)System.Math.Round((double)dataPos.x / gridSize) * gridSize;
            int y = (int)System.Math.Round((double)dataPos.y / gridSize) * gridSize;
            int z = (int)System.Math.Round((double)dataPos.z / gridSize) * gridSize;

            return new int3(x, y, z);
        }

        public static bool IsPointInCircle(int circleX, int circleY, int radius, int pointX, int pointY)
        {
            // Calculate the distance between the point and the center of the circle
            double distance =
                System.Math.Sqrt(System.Math.Pow(pointX - circleX, 2) + System.Math.Pow(pointY - circleY, 2));

            // If the distance is less than or equal to the radius then the point is inside the circle or on the boundary
            return distance <= radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int mod(int k, int n)
        {
            return ((k %= n) < 0) ? k + n : k;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlattenIndex(int3 index)
        {
            return (index.z * Chunk.ChunkSizeSquared) +
                   (index.y * Chunk.ChunkSize) +
                   index.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlattenIndex(int3 index, int chunkSize)
        {
            return (index.z * chunkSize * chunkSize) + (index.y * chunkSize) + index.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlattenIndex(int x, int y, int z)
        {
            return (z * Chunk.ChunkSizeSquared) + (y * Chunk.ChunkSize) + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlattenIndex(int x, int y, int z, int chunkSize)
        {
            return (z * chunkSize * chunkSize) + (y * chunkSize) + x;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static float3 GetChunkCenterWorldPos(int4 key)
        // {
        //     return (float3)(key.xyz) * VoxelEnvironment.VoxelSize;
        // }
        // public static float3 VoxelToWorldPosition(int3 voxelPosition)
        // {
        //     float3 voxelCenter = (float3)voxelPosition + voxelCenterPosition;
        //     return voxelCenter * VoxelEnvironment.GlobalResolutionMultiplier;
        // }
        public static int GetSizeMultiplier(this AABB aabb)
        {
            return aabb.Size.y / Chunk.ChunkSize;
        }
    }

    public struct Region : IEquatable<Region>
    {
        public int3 Origin;
        public LODLevel LODLevel;

        public Region(int3 origin, LODLevel lodLevel)
        {
            Origin = origin;
            LODLevel = lodLevel;
        }

        public static implicit operator Region(int4 value)
        {
            return new Region(value.xyz, new LODLevel(value.w));
        }

        public override bool Equals(object obj)
        {
            return obj is Region region && Equals(region);
        }

        public bool Equals(Region other)
        {
            return Origin.Equals(other.Origin) && LODLevel.Equals(other.LODLevel);
        }

        public override int GetHashCode()
        {
            var data = new int4(Origin, LODLevel.Value);
            return (int)math.hash(data);
        }

        public static Region FromAABB(AABB aabb)
        {
            var position = aabb.Min;
            var multiplier = aabb.Size.y / Chunk.ChunkSize;
            return new Region(position, LODLevel.FromMultiplier(multiplier));
        }

        public AABB ToAABB()
        {
            var min = Origin;
            var max = Origin + ((int)math.pow(2, LODLevel.Value) * Chunk.ChunkSize);
            return new AABB(min, max);
        }
    }

    public struct LODLevel : IEquatable<LODLevel>
    {
        public int Value;

        public static LODLevel FromMultiplier(int multiplier)
        {
            return new LODLevel()
            {
                Value = (int)math.log2(multiplier)
            };
        }

        public LODLevel(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is LODLevel && Equals((LODLevel)obj);
        }

        public bool Equals(LODLevel other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}