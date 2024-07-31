using System.Runtime.InteropServices;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using VoxelSystem.Colors;

namespace VoxelSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 Position;
        public float3 Normal;
        public UnityEngine.Color32 Color;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex(float x, float y, float z, float3 normal, ushort color)
        {
            Position.x = x;
            Position.y = y;
            Position.z = z;
            Normal = normal;
            Color = ColorUtilities.Convert16BitToColor32(color);
        }
    }
}