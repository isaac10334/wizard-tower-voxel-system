using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;

namespace VoxelSystem
{
    public class InteractionSystem : MonoBehaviour
    {
        public static InteractionSystem instance;
        private List<int4> _chunkKeys = new List<int4>();
        
        public void Initialize()
        {
            instance = this;
        }

        public void SetVoxelsOnChunksInRadius(float3 center, float radius)
        {
            throw new NotImplementedException();
            // // First get all the keys that this method has to touch, so all chunks within radius.
            // GetChunkCoordsInRadius(center, radius, _chunkKeys);
            //
            // // Then voxelsystem can handle gathering dependencies and do a callback when it's done.
            // _voxelEnvironment.CreateDependencyPromiseForKeys(_chunkKeys, (chunks) => OnChunkDependenciesFullfilled(chunks, center, radius));
        }
        
        // private unsafe void OnChunkDependenciesFullfilled(List<StoredChunk> dependencies, float3 center, float radius)
        // {
        //     uint blue = VoxelColor32.RGBToUint(0, 0, 255);
        //
        //     foreach(StoredChunk chunk in dependencies)
        //     {
        //         if(chunk.key.w != 1) throw new InvalidOperationException($"chunk with key {chunk.key} got inhere??");
        //         if(chunk == null) throw new InvalidOperationException("null chunk in list");
        //         if(chunk.colors == null) throw new InvalidOperationException("null chunk colors");
        //
        //         for(int i = 0; i < VoxelEnvironment.ChunkSizeCubed; i++)
        //         {
        //             int3 pos = VoxelEnvironment.Instance.LookupTables.unflattenedIndices[i];
        //             
        //             float3 voxelPosition = ((float3)chunk.key.xyz + new float3(pos.x, pos.y, pos.z)) * VoxelEnvironment.GlobalResolutionMultiplier;
        //
        //             // distance calc to make sphere
        //             float distance = math.distance(center, voxelPosition);
        //             if(distance > radius) continue;
        //
        //             chunk.colors[i] = blue;
        //         }
        //
        //         chunk.isDirty = true;
        //         chunk.wasModified = true;
        //     }
        // }
        
        // public void GetChunkCoordsInRadius(float3 position, float radius, List<int4> chunkCoords)
        // {
        //     int voxelRadius = Mathf.CeilToInt(radius / VoxelEnvironment.GlobalResolutionMultiplier);
        //     int3 voxelPosition = (int3)(position / VoxelEnvironment.GlobalResolutionMultiplier);
        //
        //     int3 startVoxelPosition = ChunkMath.FloorToGrid(voxelPosition - voxelRadius, VoxelEnvironment.VoxelsPerChunkAxis);
        //     int3 endVoxelPosition = ChunkMath.CeilToGrid(voxelPosition + voxelRadius, VoxelEnvironment.VoxelsPerChunkAxis);
        //
        //     chunkCoords.Clear();
        //
        //     // Iterate over all chunks within the start and end voxel positions
        //     for (int x = startVoxelPosition.x; x <= endVoxelPosition.x; x += VoxelEnvironment.VoxelsPerChunkAxis)
        //     {
        //         for (int y = startVoxelPosition.y; y <= endVoxelPosition.y; y += VoxelEnvironment.VoxelsPerChunkAxis)
        //         {
        //             for (int z = startVoxelPosition.z; z <= endVoxelPosition.z; z += VoxelEnvironment.VoxelsPerChunkAxis)
        //             {
        //                 int4 chunkCoord = new int4(x, y, z, 1);
        //                 chunkCoords.Add(chunkCoord);
        //             }
        //         }
        //     }
        // }
    }
}
