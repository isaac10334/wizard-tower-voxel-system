using Unity.Mathematics;

namespace VoxelSystem.Math
{
    public struct Point
    {
        public float3 Position;
        public float Radius;

        public Point(float3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    public static class Points
    {
    }
}