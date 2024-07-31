using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using System.IO;

namespace VoxelSystem
{
    public class MagicaVoxelFile : ScriptableObject
    {
        public List<SerializedChunk16> chunks;
        public string assetPath;

        public void Initialize(string assetPath)
        {
            this.assetPath = assetPath;
        }

        public unsafe void CreateFromVoxObjects(List<VoxObject> voxObjects)
        {
            // Method needs to create a dictionary mapping int3 to serializedchunk16, and loop through voxelobject,
            // finding the correct chunk index based on voxObject.position + sizeX, sizeY, and sizeZ.
            // then afterwards convert the dictionary to a list
            Dictionary<int3, SerializedChunk16> chunksMap = new Dictionary<int3, SerializedChunk16>();

            foreach (VoxObject voxObject in voxObjects)
            {
                for (int x = 0; x < voxObject.sizeX; x++)
                {
                    for (int y = 0; y < voxObject.sizeY; y++)
                    {
                        for (int z = 0; z < voxObject.sizeZ; z++)
                        {
                            uint value = voxObject.colors[x, y, z];
                            if(value == 0) continue;

                            int3 localPos = new int3(x, y, z);
                            int3 globalPos = voxObject.position + localPos;
                            int3 chunkVoxelBelongsIn = ChunkMath.FloorToGrid(globalPos, 16, out int3 withinChunkPos);
                            int withinChunkIndex = ChunkMath.FlattenIndex(withinChunkPos, 16);

                            if(!chunksMap.TryGetValue(chunkVoxelBelongsIn, out SerializedChunk16 foundChunk))
                            {
                                SerializedChunk16 chunk = new SerializedChunk16(chunkVoxelBelongsIn);
                                chunksMap.Add(chunkVoxelBelongsIn, chunk);
                            }

                            chunksMap[chunkVoxelBelongsIn].colors[withinChunkIndex] = value;
                        }
                    }
                }
            }

            chunks = new List<SerializedChunk16>(chunksMap.Values.ToList());
        }
    }
}
