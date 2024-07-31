using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;

namespace VoxelSystem
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast,
        FloatPrecision = FloatPrecision.Low)]
    public unsafe struct SurfaceNetsMesher, IVoxelMesher
    {
    public static readonly VertexAttributeDescriptor[] VertexFormat =
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32)
    };

    private ChunkWorker.Data _data;
    // public void GenerateMesh(ChunkWorker.Data data)
    // {
    // 	bool* chunkIsAir = data.isChunkAir;
    //     if(chunkIsAir[0]) return;

    //     _data = data;

    //     if (VoxelEnvironment.ChunkSize != 32)
    // 		throw new Exception("SurfaceNets requires ChunkSize == 32");

    //     VoxelToSDF volumeBuilder = new VoxelToSDF();
    // 	volumeBuilder.Convert(data.colors, data.volume);

    // 	data.bounds.item = default;
    // 	data.triangles.Clear();
    // 	data.vertices.Clear();

    // 	ProcessVoxels();

    // 	if (data.recalculateNormals)
    // 		RecalculateNormals();

    // 	// loop vertices and multiply Vertex.position by VoxelEnvironment.GlobalResolutionMultiplier

    // 	float resolution = data.Key.w * VoxelEnvironment.GlobalResolutionMultiplier;
    // 	for(int i = 0; i < data.vertices.Length; i++)
    // 	{
    // 		Vertex v = data.vertices[i];
    // 		v.position *= resolution;
    // 		data.vertices[i] = v;
    // 	}

    // 	SetupMeshData(data);

    // 	if(data.meshData.vertexCount == 0) throw new InvalidOperationException($"Meshing failed.");
    // }

    // private void SetupMeshData(NativeChunkWorker data)
    // {
    //     Mesh.MeshData meshData = data.meshData;
    //     NativeList<Vertex> vertices = data.vertices;
    //     NativeList<int> triangles = data.triangles;

    //     meshData.SetVertexBufferParams(vertices.Length, VertexFormat);
    //     NativeArray<Vertex> vBuffer = meshData.GetVertexData<Vertex>();
    //     vBuffer.CopyFrom(vertices.AsArray());

    //     meshData.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
    //     NativeArray<int> iBuffer = meshData.GetIndexData<int>();
    //     iBuffer.CopyFrom(triangles.AsArray());

    //     // One sub-mesh with all the indices.
    //     meshData.subMeshCount = 1;
    //     meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
    // }

    [SkipLocalsInit]
    unsafe void ProcessVoxels()
    {
        // Samples 01 and 02 - those are local arrays of interleaved voxel data (rows of voxels along Z coordinate)
        //
        //   imagine a slice of voxel data (YX) :
        //   
        //     Y
        //     |
        //     |
        //   [   ][   ][   ]
        //   [ 2 ][ 3 ][   ]
        //   [ 0 ][ 1 ][   ] ---- X
        //   
        //   array samples01 are 'bottom' voxels - at current Y value
        //   array samples23 are 'top' voxels - at next Y value
        //   Only one array is filled per Y loop step, because we can reuse the other one.
        //   
        //   More about interleaving voxel data in next steps.

        var samples01 = stackalloc sbyte[64];
        var samples23 = stackalloc sbyte[64];

        var volumePtr = (sbyte*)_data.volume.GetUnsafeReadOnlyPtr();

        // Reusable masks with voxels sign bits
        int mask0 = 0, mask1 = 0, mask2 = 0, mask3 = 0;

        for (int x = 0; x < VoxelEnvironment.ChunkSizeMinusOne; x++)
        {
            // (0) Because some values are saved between loop iterations (along Y), so it is needed to precalc some values.
            //
            (mask2, mask3) = ExtractSignBitsAndSamples(volumePtr, samples23, x);

            for (int y = 0; y < VoxelEnvironment.ChunkSizeMinusOne; y++)
            {
                // Samples arrays are reused in Y loop, so swap those:
                // samples01 should contain voxels at current Y coordinate.
                // samples23 should contain voxels at Y + 1 coordinate.
                // So they can be reused while iterating over Y.
                //
                var temp = samples01;
                samples01 = samples23;
                samples23 = temp;

                // Previous masks are also reused:
                //
                mask0 = mask2;
                mask1 = mask3;


                (mask2, mask3) = ExtractSignBitsAndSamples(volumePtr, samples23, x, y);


                // (6) Store all masks (4 voxels 'rows' each 32 voxels == 4x 32 bit masks) in masks simd vector variable.
                //
                v128 masks = new v128(mask0, mask1, mask2, mask3);


                // (7) Early termination check - check if there is a mix of zeroes and ones in masks.
                // (7) If not, it means whole column (32 x 2 x 2 voxels) is either under surface or above - so no need to mesh those, because meshing will not produce any triangles.
                // (7) v128(UInt32.MaxValue) is a mask (all 1) controlling which bits should be tested.
                //
                int zerosOnes = X86.Sse4_1.test_mix_ones_zeroes(masks, new v128(UInt32.MaxValue));
                if (zerosOnes == 0)
                    continue;
                // (7) test_mix_ones_zeroes : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864,5903&techs=SSE4_1&cats=Logical&ig_expand=7214


                // (8) Extract last bits from each of 4 masks and store them in upper bits 4-7 (leave bits 0-3 zeroed).
                // (8) Because movemask extract sign bits (highests bits), reversing masks in step 4 & 5 was necessary.
                //
                int cornerMask = X86.Sse.movemask_ps(masks) << 4;
                // (8) movemask_ps : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864&cats=Miscellaneous&techs=SSE&ig_expand=4878


                float* samples = stackalloc float[8];

                for (int z = 0; z < VoxelEnvironment.ChunkSizeMinusOne; z++)
                {
                    // (9) Thats why we shifted masks 4 bits to left in step (8)
                    // (9) In each iteration along Z value we can reuse masks extracted in previous step.
                    //
                    cornerMask = cornerMask >> 4;

                    // (10) Previously we extracted highests bits in masks, to extract next bits we just need to left shift them.
                    // (10) slli_epi32 shift parameter mu be const.
                    //
                    masks = X86.Sse2.slli_epi32(masks, 1);
                    // (10) slli_epi32 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274&othertechs=BMI1,BMI2&techs=SSE,SSE2,SSE3,SSSE3,SSE4_1,SSE4_2&cats=Shift&ig_expand=6537

                    // (11) Extract next 4 bits from masks to build proper CornerMask for group (2x2x2) of voxels.
                    // (11) Corner mask is 8 bit where each bit tell if specific voxel from group (2x2x2) is negative or not.
                    // (11) In step (9) we reuse currently extracted 4 bits by right shifting.
                    //
                    cornerMask |= X86.Sse.movemask_ps(masks) << 4;

                    // (12) Early termination,
                    // (12) If all bits are 0 or 1 no triangles will be produced (no edge crossing)
                    //
                    if (cornerMask == 0 || cornerMask == 255)
                        continue;

                    // (13) Extract edgemask from cornermask (edgeTable is precalculated array)
                    //
                    int edgeMask = SurfaceNetsLookupTables.surfaceNetsEdgeTable[cornerMask];


                    // (14) Collect 8 samples (voxel values) from interleaved sample arrays (01 23)
                    //
                    var zz = z + z;
                    samples[0] = samples01[zz + 0];
                    samples[1] = samples01[zz + 1];
                    samples[2] = samples23[zz + 0];
                    samples[3] = samples23[zz + 1];
                    samples[4] = samples01[zz + 2];
                    samples[5] = samples01[zz + 3];
                    samples[6] = samples23[zz + 2];
                    samples[7] = samples23[zz + 3];


                    // Indexer acces is required in next step (pos[variable]...) so cant use plain int values
                    // I could use int3 struct, but for some reasons those simple stackallocated arrays works faster. No idea why.
                    //
                    int* pos = stackalloc int[3] { x, y, z };

                    // Flip Orientation Depending on Corner Sign
                    var flipTriangle = (cornerMask & 1) != 0;

                    MeshSamples(pos, samples, edgeMask, flipTriangle);
                }
            }
        }
    }

    [SkipLocalsInit]
    unsafe (int, int) ExtractSignBitsAndSamples(sbyte* volumePtr, sbyte* samples23, int x,
        int y = -1 /* first case, outside Y loop */)
    {
        // Used for reversing voxels in simd vector in step (4)
        //
        v128 shuffleReverseByteOrder = new v128(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        // (1) Pointer is needed for SSE instructions :
        //
        var ptr = volumePtr + (x << VoxelEnvironment.xShift) + ((y + 1) << VoxelEnvironment.yShift);


        // (2) Load voxel data in parts of 16.
        // (2) 'lo' and 'hi' - SSE buffers are 16 bytes in size, but chunk is in size 32, so those values refer to first 16 voxels and second 16 voxels in a row along Z coordinate.
        //
        v128 lo2 = X86.Sse2.load_si128(ptr + 0); /* load first 16 voxels */
        v128 hi2 = X86.Sse2.load_si128(ptr + 16); /* load next  16 voxels */
        v128 lo3 = X86.Sse2.load_si128(ptr + 1024); /* load first 16 voxels on X + 1 */
        v128 hi3 = X86.Sse2.load_si128(ptr + 1040); /* load next  16 voxels on X + 1 */
        // (2) load_si128 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343&techs=SSE2&ig_expand=4294,6942,6952,6942,6942,4294&cats=Load
        //
        // todo: use Sse4_1.stream_load_si128 instead ?
        // todo: use loadu_si128 for unaligned access ?? 


        // (3) Save voxel data in local samplesArrays.
        // (3) But, instead of storing them one by one like in volume array, we interleave voxels with their neighbours on X + 1 coordinate
        // (3) The unpacklo/hi intrinsics are used to interleave provided data.
        // (3) Imagine a voxel slice (XZ) at Y == 0 :
        // 
        //		  Z								   
        //		  |								   
        //		  |								   
        //		 [ 31 ][ 1055 ]   []          =>  [ hi2 ][ hi3 ]
        //		 ...                          
        //		 [  2 ][ 1026 ]   []		  =>  [ lo2 ][ lo3 ]
        //		 [  1 ][ 1025 ]   []		
        //		 [  0 ][ 1024 ]...[] ----- X 
        // 
        //		Voxels 0-15 are loaded into lo2, voxels 16-31 into hi2.
        //		Same for voxels at X + 1 (lo3/hi3).
        //		
        //		Result of unpack intrinsics looks like:
        //		
        //		[ 1055 ]  (64 element array - samples23)
        //		[   31 ]  
        //		...		  
        //		[ 1025 ]  
        //		[    1 ]  
        //		[ 1024 ]  
        //		[    0 ]  
        //
        //	(3) Such way of storing voxels makes future steps faster,
        //		because we need to access neighbouring voxels (at X + 1) while iterating along Z coordinate,
        //		so instead of accessing 2 'arrays' we access only 1.
        //
        //  (3) Second samples array (samples01) is used in same way. More about this inside loop Y
        //
        X86.Sse2.store_si128(samples23 + 00, X86.Sse2.unpacklo_epi8(lo2, lo3));
        X86.Sse2.store_si128(samples23 + 16, X86.Sse2.unpackhi_epi8(lo2, lo3));
        X86.Sse2.store_si128(samples23 + 32, X86.Sse2.unpacklo_epi8(hi2, hi3));
        X86.Sse2.store_si128(samples23 + 48, X86.Sse2.unpackhi_epi8(hi2, hi3));
        // (3) store_si128 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596&techs=SSE2&cats=Store&ig_expand=6872
        // (3) unpack(lo/hi)_epi8 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,6090,6033&othertechs=BMI1,BMI2&techs=SSE2&cats=Swizzle&ig_expand=7355


        // (4) Shuffle bytes:
        // (4) shuffleReverseByteOrder its a SSE vector with indices controlling shuffle operation (what and where to put)
        //
        lo2 = X86.Ssse3.shuffle_epi8(lo2, shuffleReverseByteOrder);
        lo3 = X86.Ssse3.shuffle_epi8(lo3, shuffleReverseByteOrder);
        hi2 = X86.Ssse3.shuffle_epi8(hi2, shuffleReverseByteOrder);
        hi3 = X86.Ssse3.shuffle_epi8(hi3, shuffleReverseByteOrder);
        // (4) shuffle_epi8 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153&techs=SSSE3&cats=Swizzle&ig_expand=6386


        // (5) Extract sign bits from 8 bit values.
        // (5) movemask intrinsics do that, 16 voxels at a time per instruction
        // (5) Result with (4) is that, the masks are reversed (first voxel sign bit is now last)
        // (5) Each mask stores bitsigns of one voxel 'row' along Z coordinate.
        // (5) Whats important, is that the bitsigns are nagated (~)
        //
        var mask2 = (X86.Sse2.movemask_epi8(lo2) << 16 | (X86.Sse2.movemask_epi8(hi2)));
        var mask3 = (X86.Sse2.movemask_epi8(lo3) << 16 | (X86.Sse2.movemask_epi8(hi3)));
        // (5) movemask_epi8 : https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864&cats=Miscellaneous&techs=SSE2&ig_expand=4873


        // those weird mask reversing operation (4 & 5) is used later in step (8).
        return (mask2, mask3);
    }

    [SkipLocalsInit]
    unsafe void MeshSamples(int* pos, float* samples, int edgeMask, bool flipTriangle)
    {
        const int R0 = (VoxelEnvironment.ChunkSize + 1) * (VoxelEnvironment.ChunkSize + 1);

        int* R = stackalloc int[3] { R0, VoxelEnvironment.ChunkSize + 1, 1 };
        int bufferIndex = pos[2] + (VoxelEnvironment.ChunkSize + 1) * pos[1];

        if (pos[0] % 2 == 0)
        {
            bufferIndex += 1 + (VoxelEnvironment.ChunkSize + 1) * (VoxelEnvironment.ChunkSize + 2);
        }
        else
        {
            R[0] = -R[0];
            bufferIndex += VoxelEnvironment.ChunkSize + 2;
        }

        // Buffer array is used to store vertex indices from previous loop steps.
        // We are using it to obtain indices for triangle.
        _data.buffer[bufferIndex] = _data.vertices.Length;

        var position = new float3(pos[0], pos[1], pos[2]) + GetVertexPositionFromSamples(samples, edgeMask);
        _data.vertices.Add(new Vertex
        {
            position = position,
            normal = _data.recalculateNormals ? float3.zero : GetVertexNormalFromSamples(samples)
        });

        _data.bounds.item.Encapsulate(position);


        // This buffer indexing stuff (buffer array, bufferIndex, R) and triangulation comes from:
        // https://github.com/TomaszFoster/NaiveSurfaceNets/blob/bec66c7a93c5b8ad4e52adf4f3091134c4c11c74/NaiveSurfaceNets.cs#L486


        // Add Faces (Loop Over 3 Base Components)
        for (var i = 0; i < 3; i++)
        {
            // First 3 Entries Indicate Crossings on Edge
            if ((edgeMask & (1 << i)) == 0)
                continue;

            var iu = (i + 1) % 3;
            var iv = (i + 2) % 3;

            if (pos[iu] == 0 || pos[iv] == 0)
                continue;

            var du = R[iu];
            var dv = R[iv];

            _data.triangles.ResizeUninitialized(_data.triangles.Length + 6);
            var indicesPtr = (int*)_data.triangles.GetUnsafePtr() + _data.triangles.Length - 6;
            // this resizing gives a lot perf (from around ~0.420 to ~0.355 [ms])

            if (flipTriangle)
            {
                indicesPtr[0] = _data.buffer[bufferIndex]; //indices.Add(buffer[bufferIndex]);
                indicesPtr[1] = _data.buffer[bufferIndex - du - dv]; //indices.Add(buffer[bufferIndex - du - dv]);
                indicesPtr[2] = _data.buffer[bufferIndex - du]; //indices.Add(buffer[bufferIndex - du]);
                indicesPtr[3] = _data.buffer[bufferIndex]; //indices.Add(buffer[bufferIndex]);
                indicesPtr[4] = _data.buffer[bufferIndex - dv]; //indices.Add(buffer[bufferIndex - dv]);
                indicesPtr[5] = _data.buffer[bufferIndex - du - dv]; //indices.Add(buffer[bufferIndex - du - dv]);
            }
            else
            {
                indicesPtr[0] = _data.buffer[bufferIndex]; //indices.Add(buffer[bufferIndex]);
                indicesPtr[1] = _data.buffer[bufferIndex - du - dv]; //indices.Add(buffer[bufferIndex - du - dv]);
                indicesPtr[2] = _data.buffer[bufferIndex - dv]; //indices.Add(buffer[bufferIndex - dv]);
                indicesPtr[3] = _data.buffer[bufferIndex]; //indices.Add(buffer[bufferIndex]);
                indicesPtr[4] = _data.buffer[bufferIndex - du]; //indices.Add(buffer[bufferIndex - du]);
                indicesPtr[5] = _data.buffer[bufferIndex - du - dv]; //indices.Add(buffer[bufferIndex - du - dv]);
            }
        }
    }

    [SkipLocalsInit]
    unsafe static float3 GetVertexPositionFromSamples(float* samples, int edgeMask)
    {
        // Check each of 12 edges for edge crossing (different voxel signs).
        // Edge mask bits tells if there is edge crossing
        // If it is, compute crossing position as linear interpolation between 2 corner position.

        var vertPos = float3.zero;
        int edgeCrossings = 0;

        if ((edgeMask & 1) != 0)
        {
            float s0 = samples[0];
            float s1 = samples[1];
            float t = s0 / (s0 - s1);
            vertPos += new float3(t, 0, 0);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 1) != 0)
        {
            float s0 = samples[0];
            float s1 = samples[2];
            float t = s0 / (s0 - s1);
            vertPos += new float3(0, t, 0);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 2) != 0)
        {
            float s0 = samples[0];
            float s1 = samples[4];
            float t = s0 / (s0 - s1);
            vertPos += new float3(0, 0, t);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 3) != 0)
        {
            float s0 = samples[1];
            float s1 = samples[3];
            float t = s0 / (s0 - s1);
            vertPos += new float3(1, t, 0);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 4) != 0)
        {
            float s0 = samples[1];
            float s1 = samples[5];
            float t = s0 / (s0 - s1);
            vertPos += new float3(1, 0, t);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 5) != 0)
        {
            float s0 = samples[2];
            float s1 = samples[3];
            float t = s0 / (s0 - s1);
            vertPos += new float3(t, 1, 0);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 6) != 0)
        {
            float s0 = samples[2];
            float s1 = samples[6];
            float t = s0 / (s0 - s1);
            vertPos += new float3(0, 1, t);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 7) != 0)
        {
            float s0 = samples[3];
            float s1 = samples[7];
            float t = s0 / (s0 - s1);
            vertPos += new float3(1, 1, t);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 8) != 0)
        {
            float s0 = samples[4];
            float s1 = samples[5];
            float t = s0 / (s0 - s1);
            vertPos += new float3(t, 0, 1);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 9) != 0)
        {
            float s0 = samples[4];
            float s1 = samples[6];
            float t = s0 / (s0 - s1);
            vertPos += new float3(0, t, 1);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 10) != 0)
        {
            float s0 = samples[5];
            float s1 = samples[7];
            float t = s0 / (s0 - s1);
            vertPos += new float3(1, t, 1);
            ++edgeCrossings;
        }

        if ((edgeMask & 1 << 11) != 0)
        {
            float s0 = samples[6];
            float s1 = samples[7];
            float t = s0 / (s0 - s1);
            vertPos += new float3(t, 1, 1);
            ++edgeCrossings;
        }

        // calculate mean position inside 1x1x1 box
        return vertPos / edgeCrossings;
    }

    [SkipLocalsInit]
    unsafe static float3 GetVertexNormalFromSamples([NoAlias] float* samples)
    {
        // Estimate normal vector from voxel values
        float3 normal;
        normal.z = (samples[4] - samples[0])
                   + (samples[5] - samples[1])
                   + (samples[6] - samples[2])
                   + (samples[7] - samples[3]);
        normal.y = (samples[2] - samples[0])
                   + (samples[3] - samples[1])
                   + (samples[6] - samples[4])
                   + (samples[7] - samples[5]);
        normal.x = (samples[1] - samples[0])
                   + (samples[3] - samples[2])
                   + (samples[5] - samples[4])
                   + (samples[7] - samples[6]);
        return normal * -0.002f; // scale normal because sampels are in range -127 127
    }

    [SkipLocalsInit]
    unsafe void RecalculateNormals()
    {
        var verticesPtr = (Vertex*)_data.vertices.GetUnsafePtr();
        var indicesPtr = (int*)_data.triangles.GetUnsafePtr();

        var indicesLength = _data.triangles.Length;

        for (int i = 0; i < indicesLength; i += 6)
        {
            // Each 2 consecutive triangles share one edge, so we need only 4 vertices
            var idx0 = indicesPtr[i + 0];
            var idx1 = indicesPtr[i + 1];
            var idx2 = indicesPtr[i + 2];
            var idx3 = indicesPtr[i + 4];

            var vert0 = verticesPtr[idx0];
            var vert1 = verticesPtr[idx1];
            var vert2 = verticesPtr[idx2];
            var vert3 = verticesPtr[idx3];

            var tangent0 = vert1.position - vert0.position;
            var tangent1 = vert2.position - vert0.position;
            var tangent2 = vert3.position - vert0.position;

            var triangleNormal0 = math.cross(tangent0, tangent1);
            var triangleNormal1 = math.cross(tangent2, tangent0);

            if (float.IsNaN(triangleNormal0.x))
            {
                triangleNormal0 = float3.zero;
            }

            if (float.IsNaN(triangleNormal1.x))
            {
                triangleNormal1 = float3.zero;
            }

            verticesPtr[idx0].normal = verticesPtr[idx0].normal + triangleNormal0 + triangleNormal1;
            verticesPtr[idx1].normal = verticesPtr[idx1].normal + triangleNormal0 + triangleNormal1;
            verticesPtr[idx2].normal = verticesPtr[idx2].normal + triangleNormal0;
            verticesPtr[idx3].normal = verticesPtr[idx3].normal + triangleNormal1;
        }
    }

    [BurstDiscard]
    public static ushort[] PrecalculateEdgeTable()
    {
        ushort[] edgeTable = new ushort[256];
        // Edge table is a lookup array for obtaining edgemasks.
        // Cornermask should be used as an index to search proper edgemask.
        // Cornermasks are 8bit binary flags where each bit tells if specific 'corner' (voxel) has negative or positive value.
        // What edgemask is ?
        // Its a bit mask of 12 edges of a cube.
        // Specific bit is enabled, if there is a 'crossing' of corresponding edge (sign change between 2 voxels).
        // If there is a sign change, such edge can produce vertex (or at least).
        // Final vertex position is calculated as a mean position of all vertices from all 'crossed' edges.
        // Magic behind calculating that edge table is unknown to me.

        var cubeEdges = new int[24];
        int k = 0;
        for (int i = 0; i < 8; ++i)
        {
            for (int j = 1; j <= 4; j <<= 1)
            {
                int p = i ^ j;
                if (i <= p)
                {
                    cubeEdges[k++] = i;
                    cubeEdges[k++] = p;
                }
            }
        }

        for (int i = 0; i < 256; ++i)
        {
            int em = 0;
            for (int j = 0; j < 24; j += 2)
            {
                var a = Convert.ToBoolean(i & (1 << cubeEdges[j]));
                var b = Convert.ToBoolean(i & (1 << cubeEdges[j + 1]));
                em |= a != b ? (1 << (j >> 1)) : 0;
            }

            edgeTable[i] = (ushort)em;
        }

        return edgeTable;
    }
    }

    public unsafe struct VoxelToSDF
    {
        public const int MaxDistance = 127;
        [ReadOnly] public uint* colors;
        public NativeArray<sbyte> volume;

        public void Convert(uint* colors, NativeArray<sbyte> volume)
        {
            this.colors = colors;
            this.volume = volume;

            DistanceFieldGenerator.ChamferDistanceTransform(volume, colors);
            // ConstructSignedDistanceField(colors, volume);
        }

        public void ConstructSignedDistanceField(uint* voxels, NativeArray<sbyte> volume)
        {
            int chunkSize = VoxelEnvironment.ChunkSize;
            int chunkSizeSquared = chunkSize * chunkSize;
            // int size = VoxelEnvironment.ChunkSizeCubed;

            // Step 1: Create a new NativeArray<sbyte> volume
            // with the same size as the NativeArray<uint> voxels.
            // for (int i = 0; i < size; i++)
            // {
            // 	volume[i] = (sbyte)0;
            // }

            // Step 2: Iterate through the voxels and for each voxel
            // that is a solid (nonzero), calculate the distance to
            // the nearest air voxel using the Chamfer Distance Transform.
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int flatIndex = x * chunkSizeSquared + y * chunkSize + z;
                        if (voxels[flatIndex] != 0)
                        {
                            float dist = ChamferDistanceTransform(voxels, x, y, z);
                            // volume[flatIndex] = (sbyte)Mathf.Clamp(dist * -127, -127, 127);
                            float val = math.clamp(dist, -1.0f, 1.0f) * -127;
                            volume[flatIndex] = (sbyte)val;
                        }
                    }
                }
            }
        }

        // Helper method to calculate the Chamfer Distance Transform
        // for a given voxel and return the distance to the nearest air voxel.
        private float ChamferDistanceTransform(uint* voxels, int x, int y, int z)
        {
            int chunkSize = VoxelEnvironment.ChunkSize;
            int chunkSizeSquared = chunkSize * chunkSize;

            float minDist = float.MaxValue;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        if (i == 0 && j == 0 && k == 0)
                        {
                            continue;
                        }

                        int nx = x + i;
                        int ny = y + j;
                        int nz = z + k;

                        if (nx < 0 || nx >= chunkSize ||
                            ny < 0 || ny >= chunkSize ||
                            nz < 0 || nz >= chunkSize)
                        {
                            continue;
                        }

                        int flatIndex = nx * chunkSizeSquared + ny * chunkSize + nz;
                        if (voxels[flatIndex] == 0)
                        {
                            float dist = Mathf.Sqrt(i * i + j * j + k * k);
                            minDist = Mathf.Min(minDist, dist);
                        }
                    }
                }
            }

            return minDist;
        }

        // public void MakeSphere(NativeArray<sbyte> volume)
        // {
        // 	for(int x = 0; x < 32; x++)
        // 	{
        // 		var flatIndex = x * VoxelEnvironment.ChunkSize * VoxelEnvironment.ChunkSize;
        // 		var sphereRadius = 14.4f + math.sin(1f);

        // 		for (int y = 0; y < VoxelEnvironment.ChunkSize; y++)
        // 		{
        // 			for (int z = 0; z < VoxelEnvironment.ChunkSize; z++)
        // 			{
        // 				var voxelPosition = new float3(x, y, z);
        // 				var val = math.distance(voxelPosition, new float3(15.5f)) - sphereRadius;
        // 				val = math.clamp(val, -1.0f, 1.0f) * -127;
        // 				volume[flatIndex] = (sbyte)val;
        // 				flatIndex++;
        // 			}
        // 		}
        // 	}
        // }

        [BurstDiscard]
        public static int[] CalculateChamferLookupTable()
        {
            int[] table = new int[256];
            for (int i = 0; i < 256; i++)
            {
                table[i] = i < 128 ? i * i : (i - 255) * (i - 255);
            }

            return table;
        }
    }

    public unsafe class DistanceFieldGenerator
    {
        public static void ChamferDistanceTransform(NativeArray<sbyte> volume, uint* voxels)
        {
            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };
            int[] w = { 1, 1, 1, 1, 1, 1 };

            for (int i = 0; i < VoxelEnvironment.VoxelsPerChunk; i++)
            {
                volume[i] = voxels[i] == 0 ? (sbyte)127 : (sbyte)-127;
            }

            NativeArray<sbyte> tempVolume = new NativeArray<sbyte>(volume.Length, Allocator.Temp);

            for (int iter = 0; iter < 2; iter++)
            {
                tempVolume.CopyFrom(volume);

                for (int x = 0; x < VoxelEnvironment.VoxelsPerChunkAxis; x++)
                {
                    for (int y = 0; y < VoxelEnvironment.VoxelsPerChunkAxis; y++)
                    {
                        for (int z = 0; z < VoxelEnvironment.VoxelsPerChunkAxis; z++)
                        {
                            // int flatIndex = x * VoxelEnvironment.ChunkSizeSquared + y * VoxelEnvironment.ChunkSize + z;
                            int flatIndex = z * VoxelEnvironment.VoxelsPerChunkSlice +
                                            y * VoxelEnvironment.VoxelsPerChunkAxis + x;

                            for (int d = 0; d < 6; d++)
                            {
                                int nx = x + dx[d];
                                int ny = y + dy[d];
                                int nz = z + dz[d];

                                if (nx >= 0 && nx < VoxelEnvironment.VoxelsPerChunkAxis &&
                                    ny >= 0 && ny < VoxelEnvironment.VoxelsPerChunkAxis &&
                                    nz >= 0 && nz < VoxelEnvironment.VoxelsPerChunkAxis)
                                {
                                    int neighborFlatIndex = nz * VoxelEnvironment.VoxelsPerChunkSlice +
                                                            ny * VoxelEnvironment.VoxelsPerChunkAxis + nx;

                                    // int newVal = volume[neighborFlatIndex] + w[d];
                                    int newVal = tempVolume[neighborFlatIndex] + w[d];

                                    if (volume[flatIndex] > 0)
                                    {
                                        volume[flatIndex] = (sbyte)math.min(volume[flatIndex], newVal);
                                    }
                                    else
                                    {
                                        volume[flatIndex] = (sbyte)math.max(volume[flatIndex], -newVal);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}