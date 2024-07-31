using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem.Chunks
{
    public struct ChunkMesh
    {
        public Mesh Mesh;
        public Bounds Bounds;

        public ChunkMesh(Bounds bounds)
        {
            Bounds = bounds;
            Mesh = null;
        }
    }
}