using System;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    public struct NeighborTables
    {
        [ReadOnly] public NativeArray<int> niXN;
        [ReadOnly] public NativeArray<int> niYN;
        [ReadOnly] public NativeArray<int> niZN;
        [ReadOnly] public NativeArray<int> niXP;
        [ReadOnly] public NativeArray<int> niYP;
        [ReadOnly] public NativeArray<int> niZP;

        public NeighborTables(Allocator allocator)
        {
            niXN = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
            niYN = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
            niZN = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
            niXP = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
            niYP = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
            niZP = new NativeArray<int>(Chunk.ChunkSizeCubed, allocator);
        }
        public void Dispose()
        {
            niXN.Dispose();
            niYN.Dispose();
            niZN.Dispose();
            niXP.Dispose();
            niYP.Dispose();
            niZP.Dispose();
        }

        internal void Generate()
        {
            int cs = Chunk.ChunkSize;
            int cs2 = cs*cs;
            int cs3 = cs2 * cs;

            for(int x = 0; x < cs; x++)
            {
                for(int y = 0; y < cs; y++)
                {
                    for(int z = 0; z < cs; z++)
                    {
                        int pz = (z * cs2);
                        int py = (y * cs);
                        int pzy = pz + py;
                        int flat = pzy + x;

                        niXN[flat] = x == 0 ? -1 : (z*cs*cs)+(y*cs)+(x-1);
                        niYN[flat] = y == 0 ? -1 : (z*cs*cs)+((y-1)*cs)+x;
                        niZN[flat] = z == 0 ? -1 : ((z-1)*cs*cs)+(y*cs)+x;
                        niXP[flat] = x == cs-1 ? -2 : (z*cs*cs)+(y*cs)+(x+1);
                        niYP[flat] = y == cs-1 ? -2 : (z*cs*cs)+((y+1)*cs)+x;
                        niZP[flat] = z == cs-1 ? -2 : ((z+1)*cs*cs)+(y*cs)+x;
                    }
                }
            }
        }
    }
    public static class LookupTableMath
    {
        // public static readonly int3[]  offsets = new int3[6];
        // for(int face = 0; face < 6; face++)
        // {
        //     int direction = face % 3;

        //     int3 temp = new int3();
        //     offsets[direction] = face > 2 ? 1 : -1;
        //     offsets[face] = temp;
        // }
        
        public static NativeArray<int3> GenerateUnflattenedIndicesTable(Allocator allocator)
        {
            NativeArray<int3> unflattenedIndices = new NativeArray<int3>(Chunk.ChunkSizeCubed, allocator, NativeArrayOptions.UninitializedMemory);
            
            int cs = Chunk.ChunkSize;
            int cs2 = cs*cs;
            int cs3 = cs2 * cs;

            for(int x = 0; x < cs; x++)
            {
                for(int y = 0; y < cs; y++)
                {
                    for(int z = 0; z < cs; z++)
                    {
                        int3 pos = new int3(x, y, z);

                        int pz = (z * cs2);
                        int py = (y * cs);
                        int pzy = pz + py;

                        int flat = pzy + x;

                        unflattenedIndices[flat] = pos;
                    }
                }
            }

            return unflattenedIndices;
        }
        public static NativeArray<int3> GenerateIndexComponentsTable(Allocator allocator)
        {
            NativeArray<int3> indexComponents = new NativeArray<int3>(Chunk.ChunkSizeCubed, allocator, NativeArrayOptions.UninitializedMemory);
            
            int cs = Chunk.ChunkSize;
            int cs2 = cs*cs;
            int cs3 = cs2 * cs;

            for(int x = 0; x < cs; x++)
            {
                for(int y = 0; y < cs; y++)
                {
                    for(int z = 0; z < cs; z++)
                    {
                        int pz = (z * cs2);
                        int py = (y * cs);
                        int pzy = pz + py;
                        int flat = pzy + x;
                        indexComponents[flat] = new int3(pzy, pz, py);
                    }
                }
            }

            return indexComponents;
        }
        public static NeighborTables GenerateNeighborTables(Allocator allocator)
        {
            NeighborTables neighborTables = new NeighborTables(allocator);
            neighborTables.Generate();
            return neighborTables;
        }
    }
}
