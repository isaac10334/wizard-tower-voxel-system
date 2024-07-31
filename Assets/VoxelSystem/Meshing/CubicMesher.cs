using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace VoxelSystem.Meshing
{
    // A meshing job that creates runs to reduce overall vertices but isn't fully greedy, inspired by https://vercidium.com/blog/voxel-world-optimisations/
    public unsafe struct CubicGreedyMeshCreator : IFancyJob<Mesh>
    {
        private VoxelMeshData _voxelMeshData;
        private NativeLookupTableData _lookupTables;
        private Chunk _chunk;

        public static CubicGreedyMeshCreator Create(Chunk chunk)
        {
            return new CubicGreedyMeshCreator { _chunk = chunk };
        }

        [BurstDiscard]
        public void OnBeforeSchedule()
        {
            _voxelMeshData = ConfigureableObjectPool<VoxelMeshData>.Instance.GetObject();
            _lookupTables = LookupTables.Data;
        }

        [BurstDiscard]
        public Mesh CompleteAndReturn()
        {
            var mesh = ManagedObjectPool<Mesh>.Instance.GetObject();
            mesh.Clear();

            _voxelMeshData.Apply(mesh);
            // TODO keep bounds on chunk itself and just use that
            mesh.RecalculateBounds();

            ConfigureableObjectPool<VoxelMeshData>.Instance.ReturnObject(_voxelMeshData);
            return mesh;
        }

        public void Execute()
        {
            var masks = new Masks(Chunk.ChunkSizeCubed * sizeof(bool), Allocator.Temp, true, 0);

            BuildVoxelMesh(masks);
            var meshData = _voxelMeshData.MeshDataArray[0];

            NativeArray<VertexAttributeDescriptor> vertexAttributes = CreateVertexAttributesDescriptor();
            meshData.SetVertexBufferParams(_voxelMeshData.Vertices.Length, vertexAttributes);
            NativeArray<Vertex> v = meshData.GetVertexData<Vertex>();
            v.CopyFrom(_voxelMeshData.Vertices.AsArray());

            meshData.SetIndexBufferParams(_voxelMeshData.Triangles.Length, IndexFormat.UInt32);
            NativeArray<int> i = meshData.GetIndexData<int>();
            i.CopyFrom(_voxelMeshData.Triangles.AsArray());

            // One sub-mesh with all the indices.
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, _voxelMeshData.Triangles.Length),
                VoxelMeshData.UpdateFlags);
        }

        private NativeArray<VertexAttributeDescriptor> CreateVertexAttributesDescriptor()
        {
            NativeArray<VertexAttributeDescriptor> vertexAttributes =
                new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp);

            vertexAttributes[0] = VoxelMeshData.VertexAttributes[0];
            vertexAttributes[1] = VoxelMeshData.VertexAttributes[1];
            vertexAttributes[2] = VoxelMeshData.VertexAttributes[2];

            return vertexAttributes;
        }

        private void BuildVoxelMesh(Masks masks)
        {
            int3 unflattened;
            int3 comps;
            int pzy, pz, py;
            ushort voxelID;
            int neighborIndexXN, neighborIndexYN, neighborIndexZN, neighborIndexXP, neighborIndexYP, neighborIndexZP;

            ushort color;

            for (int flat = 0; flat < Chunk.ChunkSizeCubed; flat++)
            {
                voxelID = _chunk.GetColor(flat);
                if (voxelID == 0) continue;

                unflattened = _lookupTables.unflattenedIndices[flat];
                comps = _lookupTables.indexComponents[flat];
                pzy = comps[0];
                pz = comps[1];
                py = comps[2];

                color = voxelID;

                var xf = (float)(unflattened.x);
                var yf = (float)(unflattened.y);
                var zf = (float)(unflattened.z);

                var xf1 = xf + 1;
                var yf1 = yf + 1;
                var zf1 = zf + 1;

                int length, newIndex;

                // XN
                if (!masks.xNegativeMask[flat])
                {
                    neighborIndexXN = _lookupTables.neighborsTable.niXN[flat];

                    if (neighborIndexXN == -1 || _chunk.GetColor(neighborIndexXN) == 0)
                    {
                        for (length = unflattened.y; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pz + length * Chunk.ChunkSize + unflattened.x;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.xNegativeMask[newIndex] = true;
                        }

                        AddSquareFaceX(color, xf, yf, length, zf, zf1, ChunkMath.XN);
                        AddFrontFacingIndices();
                    }
                }

                //XP
                if (!masks.xPositiveMask[flat])
                {
                    neighborIndexXP = _lookupTables.neighborsTable.niXP[flat];

                    if (neighborIndexXP == -2 || _chunk.GetColor(neighborIndexXP) == 0)
                    {
                        for (length = unflattened.y; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pz + length * Chunk.ChunkSize + unflattened.x;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.xPositiveMask[newIndex] = true;
                        }

                        AddSquareFaceX(color, xf1, yf, length, zf, zf1, ChunkMath.XP);
                        AddBackFacingIndices();
                    }
                }

                //YN
                if (!masks.yNegativeMask[flat])
                {
                    neighborIndexYN = _lookupTables.neighborsTable.niYN[flat];

                    if (neighborIndexYN == -1 || _chunk.GetColor(neighborIndexYN) == 0)
                    {
                        for (length = unflattened.x; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pzy + length;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.yNegativeMask[newIndex] = true;
                        }

                        AddSquareFaceY(color, xf, length, yf, zf, zf1, ChunkMath.YN);
                        AddBackFacingIndices();
                    }
                }

                //YP
                if (!masks.yPositiveMask[flat])
                {
                    neighborIndexYP = _lookupTables.neighborsTable.niYP[flat];

                    if (neighborIndexYP == -2 || _chunk.GetColor(neighborIndexYP) == 0)
                    {
                        for (length = unflattened.x; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pzy + length;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.yPositiveMask[newIndex] = true;
                        }

                        AddSquareFaceY(color, xf, length, yf1, zf, zf1, ChunkMath.YP);
                        AddFrontFacingIndices();
                    }
                }

                //ZN
                if (!masks.zNegativeMask[flat])
                {
                    neighborIndexZN = _lookupTables.neighborsTable.niZN[flat];

                    if (neighborIndexZN == -1 || _chunk.GetColor(neighborIndexZN) == 0)
                    {
                        for (length = unflattened.y; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pz + length * Chunk.ChunkSize + unflattened.x;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.zNegativeMask[newIndex] = true;
                        }

                        AddSquareFaceZ(color, xf1, xf, yf, length, zf, ChunkMath.ZN);
                        AddBackFacingIndices();
                    }
                }

                //ZP
                if (!masks.zPositiveMask[flat])
                {
                    neighborIndexZP = _lookupTables.neighborsTable.niZP[flat];

                    if (neighborIndexZP == -2 || _chunk.GetColor(neighborIndexZP) == 0)
                    {
                        for (length = unflattened.y; length < Chunk.ChunkSize; length++)
                        {
                            newIndex = pz + length * Chunk.ChunkSize + unflattened.x;
                            if (_chunk.GetColor(newIndex) != voxelID) break;
                            masks.zPositiveMask[newIndex] = true;
                        }

                        AddSquareFaceZ(color, xf, xf1, yf, length, zf1, ChunkMath.ZP);
                        AddBackFacingIndices();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSquareFaceX(ushort color, float x, float yn,
            float yp, float zn, float zp, float3 normal)
        {
            var ptr = GetNewFacePtr();
            ptr[0] = new Vertex(x, yn, zn, normal, color);
            ptr[1] = new Vertex(x, yn, zp, normal, color);
            ptr[2] = new Vertex(x, yp, zp, normal, color);
            ptr[3] = new Vertex(x, yp, zn, normal, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSquareFaceY(ushort color, float xn,
            float xp, float y, float zn, float zp, float3 normal)
        {
            var ptr = GetNewFacePtr();
            ptr[0] = new Vertex(xn, y, zn, normal, color);
            ptr[1] = new Vertex(xn, y, zp, normal, color);
            ptr[2] = new Vertex(xp, y, zp, normal, color);
            ptr[3] = new Vertex(xp, y, zn, normal, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSquareFaceZ(ushort color, float xn,
            float xp, float yn, float yp, float z, float3 normal)
        {
            var ptr = GetNewFacePtr();
            ptr[0] = new Vertex(xn, yn, z, normal, color);
            ptr[1] = new Vertex(xn, yp, z, normal, color);
            ptr[2] = new Vertex(xp, yp, z, normal, color);
            ptr[3] = new Vertex(xp, yn, z, normal, color);
        }

        private Vertex* GetNewFacePtr()
        {
            _voxelMeshData.Vertices.ResizeUninitialized(_voxelMeshData.Vertices.Length + 4);
            return _voxelMeshData.Vertices.GetUnsafePtr() + _voxelMeshData.Vertices.Length - 4;
        }

        private void AddFrontFacingIndices()
        {
            var count = _voxelMeshData.Vertices.Length;
            _voxelMeshData.Triangles.ResizeUninitialized(_voxelMeshData.Triangles.Length + 6);
            var indicesPtr = (int*)_voxelMeshData.Triangles.GetUnsafePtr() + _voxelMeshData.Triangles.Length - 6;

            indicesPtr[0] = (count - 4);
            indicesPtr[1] = (count - 3);
            indicesPtr[2] = (count - 2);
            indicesPtr[3] = (count - 4);
            indicesPtr[4] = (count - 2);
            indicesPtr[5] = (count - 1);
        }

        private void AddBackFacingIndices()
        {
            var count = _voxelMeshData.Vertices.Length;
            _voxelMeshData.Triangles.ResizeUninitialized(_voxelMeshData.Triangles.Length + 6);
            var indicesPtr = _voxelMeshData.Triangles.GetUnsafePtr() + _voxelMeshData.Triangles.Length - 6;

            indicesPtr[0] = (count - 2);
            indicesPtr[1] = (count - 3);
            indicesPtr[2] = (count - 4);
            indicesPtr[3] = (count - 1);
            indicesPtr[4] = (count - 2);
            indicesPtr[5] = (count - 4);
        }
    }
}