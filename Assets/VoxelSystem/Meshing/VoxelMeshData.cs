using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem.Meshing
{
    public struct VoxelMeshData : IPoolable
    {
        public const MeshUpdateFlags UpdateFlags = MeshUpdateFlags.DontNotifyMeshUsers |
                                                   MeshUpdateFlags.DontResetBoneBounds |
                                                   MeshUpdateFlags.DontValidateIndices;

        public static readonly VertexAttributeDescriptor[] VertexAttributes = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        };

        public NativeList<Vertex> Vertices;
        public NativeList<int> Triangles;
        public Mesh.MeshDataArray MeshDataArray;
        private bool _meshDataAllocated;

        public void OnCreate()
        {
            Vertices = new NativeList<Vertex>(10000, Allocator.Domain);
            Triangles = new NativeList<int>(10000, Allocator.Domain);

            if (_meshDataAllocated)
            {
                MeshDataArray.Dispose();
            }

            MeshDataArray = Mesh.AllocateWritableMeshData(1);
            _meshDataAllocated = true;
        }

        // recreated every use, not poolable.
        public void OnRetrievedFromPool()
        {
            if (_meshDataAllocated) return;

            MeshDataArray = Mesh.AllocateWritableMeshData(1);
            _meshDataAllocated = true;
        }

        public void OnReturnedToPool()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        public void Apply(Mesh mesh)
        {
            Mesh.ApplyAndDisposeWritableMeshData(MeshDataArray, mesh, VoxelMeshData.UpdateFlags);
            _meshDataAllocated = false;
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Triangles.Dispose();
            if (_meshDataAllocated)
            {
                MeshDataArray.Dispose();
                _meshDataAllocated = false;
            }
        }
    }
}