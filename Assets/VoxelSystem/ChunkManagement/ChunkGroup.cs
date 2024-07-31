using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace VoxelSystem
{
    public unsafe struct Chunk16
    {
        public const int ChunkSize = 16;
        public const int ChunkSizeSquared = 256;
        public const int ChunkSizeCubed = 4096;
        public int3 position;
        public uint* colorsPtr;
        private Allocator _allocatorHandle;
        public Chunk16(Allocator allocator)
        {
            position = int3.zero;
            _allocatorHandle = allocator;
            colorsPtr = (uint*)UnsafeUtility.Malloc(ChunkSizeCubed * sizeof(uint), 4, allocator);
            UnsafeUtility.MemClear(colorsPtr, ChunkSizeCubed * sizeof(uint));
        }
        public void Dispose()
        {
            UnsafeUtility.Free(colorsPtr, _allocatorHandle);
        }
    }
    
    [Serializable]
    public struct SerializedChunk16
    {
        public int3 position;
        public uint[] colors;
        
        public SerializedChunk16(int3 position)
        {
            this.position = position;
            colors = new uint[Chunk16.ChunkSizeCubed];
        }

        public unsafe Chunk16 ToChunk16(Allocator allocator)
        {
            Chunk16 chunk = new Chunk16(allocator);
            chunk.position = position;

            for(int i = 0; i < colors.Length; i++)
            {
                chunk.colorsPtr[i] = colors[i];
            }

            return chunk;
        }
    }
}
