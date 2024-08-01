using System;
using FishNet.Object;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem
{
    public class VoxelManager : NetworkBehaviour
    {
        public static VoxelManager Instance;

        private const string SimpleTerrainNoise =
            "EQAIAAAArkcBQBAAzcwMQBkAGQATAMP1KD8NAAQAAAAAACBACQAAZmYmPwAAAAA/AQQAAAAAAI/CtT8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABGwAZABMArkdhvg0AAgAAAJqZmT8JAAAK16O9AI/Cdb0BBAAAAAAASOF6PwAAAAAAAAAAAAAAAM3MzL0AAAAAAAAAAACuR4FAAClcjz4APQpXPwDNzAxA";

        private NativeHashMap<Region, Chunk> _chunks;
        private bool _allocated = false;

        private void Awake()
        {
            Instance ??= this;
        }

        public override void OnStartServer()
        {
            if (_allocated) throw new InvalidOperationException("Already allocated.");
            _chunks = new NativeHashMap<Region, Chunk>(4096, Allocator.Domain);
            _allocated = true;
        }

        public static bool BoxCast()
        {
            // Vector3 center,
            //     Vector3 halfExtents,
            // Vector3 direction,
            //     out RaycastHit hitInfo,
            throw new NotImplementedException();
        }

        public static bool OverlapBox(AABB aabb)
        {
            throw new NotImplementedException();
        }

        public static bool ComputePenetration()
        {
            throw new NotImplementedException();
        }

        public static float3 FindAreaOnSurface(AABB playerAabb, float3 point)
        {
            // recursively call this with random positions around here on surface
            // it would be inefficient not to try to directly find the surface
            // so do that.

            // This requires the overlapbox method I wanted.
            throw new NotImplementedException("That's complicated.");
        }


        public bool IsChunkAir(Region region)
        {
            return true;
        }

        // Future - all one type, or a consistent repeating material generatable from a simple function, or something.


        [Server]
        public Chunk GetChunk(Region region)
        {
            if (_chunks.TryGetValue(region, out Chunk chunk))
            {
                Debug.Log("Chunk was cached in server memory!");
                // this is legitimately all we gotta do
                // important - don't return data if they can infer it
                return chunk;
            }
            else
            {
                // construct a chunk from the database
                // if it's not there, that means it's either air or can be generated
                return new Chunk()
                {
                    AreaInformation = RegionInformation.Unmodified,
                    Aabb = region.ToAABB(),
                };
            }

            throw new NotImplementedException("Could not resolve chunk.");
        }

        public struct DataGenerator : IJob
        {
            private Unity.Mathematics.Random _random;
            private const float Surface = -0.6f;

            private Chunk _chunk;

            public static DataGenerator Create(Chunk chunk)
            {
                return new DataGenerator() { _chunk = chunk };
            }

            public void Execute()
            {
                _chunk.ColorVoxelsBelowSurface(SimpleTerrainNoise, Surface, 0.005f, 123,
                    new Color32(90, 105, 80, 255),
                    new Color32(80, 120, 85, 255),
                    new Color32(85, 115, 90, 255),
                    new Color32(75, 110, 75, 255));
            }
        }
    }
}