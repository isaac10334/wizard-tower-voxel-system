using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityS.Physics;

namespace VoxelSystem
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct MeshColliderGeneratorJob : IJob, IDisposable
    {
        public NativeReference<PhysicsCollider> PhysicsCollider;
        private NativeList<Vertex> _vertices;
        private NativeList<int> _indices;

        [BurstDiscard]
        public static MeshColliderGeneratorJob GetColliderJob(NativeList<Vertex> vertices, NativeList<int> indices)
        {
            return new MeshColliderGeneratorJob()
            {
                _vertices = vertices,
                _indices = indices,
            };
        }

        public void Execute()
        {
            // // Set PhysicsCollider
            // var vertices = chunkTask.GetVertexPositions();
            // new NativeArray<float3>(_vertices.Length, Allocator.Temp);
            // for (int i = 0; i < _vertices.Length; i++)
            // {
            //     vertices[i] = _vertices[i].Position;
            // }
            //
            // var triangles = new NativeArray<int3>(_indices.Length / 3, Allocator.Temp);
            // // Map the flat int array to NativeArray<int3>
            // for (int i = 0; i < _indices.Length; i += 3)
            // {
            //     triangles[i / 3] = new int3(_indices[i], _indices[i + 1], _indices[i + 2]);
            // }
            //
            // var collider = Unity.Physics.MeshCollider.Create(vertices, triangles);
            // var physicsCollider = new PhysicsCollider { Value = collider };
            // vertices.Dispose();
            // triangles.Dispose();
        }

        public void Dispose()
        {
            PhysicsCollider.Dispose();
        }
    }
}