using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static unsafe class FastNoise
{
    public const string NATIVE_LIB = "FastVoxels";

    public static string GetEncodedNodeDataTree(IntPtr nodeDataHandle)
    {
        if (nodeDataHandle == IntPtr.Zero) return String.Empty;

        string encodedNodeTree = Marshal.PtrToStringAnsi(fnGetEncodedNodeTree(nodeDataHandle));
        return encodedNodeTree;
    }

    public static float FindSurface3D(FixedString512Bytes tree, float x, float z, int seed, float threshold,
        float minHeight = -500,
        float maxHeight = 500)
    {
        float low = minHeight;
        float high = maxHeight;
        float mid = 0f;

        var node = FastNoise.fnNewFromEncodedNodeTree(tree.GetUnsafePtr());
        if (node == IntPtr.Zero) throw new ArgumentException(nameof(tree));

        // Ensure that we start with one point above and one below the threshold
        float noiseLow = FastNoise.fnGenSingle3D(node, x, low, z, seed);
        float noiseHigh = FastNoise.fnGenSingle3D(node, x, high, z, seed);

        if (noiseLow > threshold && noiseHigh < threshold)
        {
            // Swap if necessary to ensure low is below the surface and high is above
            float temp = low;
            low = high;
            high = temp;
        }
        else if (!(noiseLow < threshold && noiseHigh > threshold))
        {
            FastNoise.fnDeleteNodeRef(node);

            // If both are above or below the surface, we cannot find a surface between them
            return -1; // Indicates an error or no surface found
        }

        // Binary search for the surface threshold
        while (high - low > 0.1)
        {
            mid = (low + high) / 2;
            float noiseMid = FastNoise.fnGenSingle3D(node, x, mid, z, seed);

            if (noiseMid > threshold) // We're above the surface
            {
                low = mid;
            }
            else // We're below the surface
            {
                high = mid;
            }
        }

        FastNoise.fnDeleteNodeRef(node);

        // Mid is now the approximate height of the surface
        return mid;
    }

    #region Gen

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnNewFromMetadata(int id, uint simdLevel = 0);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnNewFromEncodedNodeTree([MarshalAs(UnmanagedType.LPStr)] string encodedNodeTree,
        uint simdLevel = 0);

    [DllImport(NATIVE_LIB)]
    public unsafe static extern IntPtr fnNewFromEncodedNodeTree(byte* encodedNodeTree, uint simdLevel = 0);

    [DllImport(NATIVE_LIB)]
    public static extern void fnDeleteNodeRef(IntPtr nodeHandle);

    [DllImport(NATIVE_LIB)]
    public static extern void fnDeleteNodeRef(void* nodeHandle);

    [DllImport(NATIVE_LIB)]
    public static extern uint fnGetSIMDLevel(IntPtr nodeHandle);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataID(IntPtr nodeHandle);

    [DllImport(NATIVE_LIB)]
    public static extern uint fnGenUniformGrid2D(void* nodeHandle, float* noiseOut,
        int xStart, int yStart,
        int xSize, int ySize,
        float frequency, int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern uint fnGenUniformGrid2D(IntPtr nodeHandle, float[] noiseOut,
        int xStart, int yStart,
        int xSize, int ySize,
        float frequency, int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern uint fnGenUniformGrid3D(IntPtr nodeHandle, float* noiseOut,
        int xStart, int yStart, int zStart,
        int xSize, int ySize, int zSize,
        float frequency, int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern uint fnGenUniformGrid4D(IntPtr nodeHandle, float* noiseOut,
        int xStart, int yStart, int zStart, int wStart,
        int xSize, int ySize, int zSize, int wSize,
        float frequency, int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern void fnGenTileable2D(IntPtr node, float* noiseOut,
        int xSize, int ySize,
        float frequency, int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern void fnGenPositionArray2D(IntPtr node, float* noiseOut, int count,
        float* xPosArray, float* yPosArray,
        float xOffset, float yOffset,
        int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern void fnGenPositionArray3D(IntPtr node, float* noiseOut, int count,
        float* xPosArray, float* yPosArray, float* zPosArray,
        float xOffset, float yOffset, float zOffset,
        int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern void fnGenPositionArray4D(IntPtr node, float* noiseOut, int count,
        float* xPosArray, float* yPosArray, float* zPosArray, float* wPosArray,
        float xOffset, float yOffset, float zOffset, float wOffset,
        int seed, float* outputMinMax);

    [DllImport(NATIVE_LIB)]
    public static extern float fnGenSingle2D(void* node, float x, float y, int seed);

    [DllImport(NATIVE_LIB)]
    public static extern float fnGenSingle3D(IntPtr node, float x, float y, float z, int seed);

    [DllImport(NATIVE_LIB)]
    public static extern float fnGenSingle4D(IntPtr node, float x, float y, float z, float w, int seed);

    #endregion

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataCount();

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataName(int id);

    // Variable
    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataVariableCount(int id);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataVariableName(int id, int variableIndex);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataVariableType(int id, int variableIndex);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataVariableDimensionIdx(int id, int variableIndex);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataEnumCount(int id, int variableIndex);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataEnumName(int id, int variableIndex, int enumIndex);

    [DllImport(NATIVE_LIB)]
    public static extern bool fnSetVariableFloat(IntPtr nodeHandle, int variableIndex, float value);

    [DllImport(NATIVE_LIB)]
    public static extern bool fnSetVariableIntEnum(IntPtr nodeHandle, int variableIndex, int value);

    // Node Lookup
    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataNodeLookupCount(int id);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataNodeLookupName(int id, int nodeLookupIndex);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataNodeLookupDimensionIdx(int id, int nodeLookupIndex);

    [DllImport(NATIVE_LIB)]
    public static extern bool fnSetNodeLookup(IntPtr nodeHandle, int nodeLookupIndex, IntPtr nodeLookupHandle);

    // Hybrid
    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataHybridCount(int id);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataHybridName(int id, int nodeLookupIndex);

    [DllImport(NATIVE_LIB)]
    public static extern int fnGetMetadataHybridDimensionIdx(int id, int nodeLookupIndex);

    [DllImport(NATIVE_LIB)]
    public static extern bool fnSetHybridNodeLookup(IntPtr nodeHandle, int nodeLookupIndex, IntPtr nodeLookupHandle);

    [DllImport(NATIVE_LIB)]
    public static extern bool fnSetHybridFloat(IntPtr nodeHandle, int nodeLookupIndex, float value);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataGroupName(int id);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetMetadataDescription(int id);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnDeserializeNodeDataVector([MarshalAs(UnmanagedType.LPStr)] string encodedNodeTree);

    [DllImport(NATIVE_LIB)]
    public static extern IntPtr fnGetRootNodeDataFromVector(IntPtr vectorPtr);

    [DllImport(NATIVE_LIB)]
    public static extern void fnDeleteNodeDataVector(IntPtr vectorPtr);

    [DllImport(NATIVE_LIB)]
    public static extern void fnRemoveNodeDataFromVector(IntPtr vectorPtr, IntPtr nodeDataPtr);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnGetNodeDataVectorHandleFromMetadata(int id);

    [DllImport(FastNoise.NATIVE_LIB)]
    private static extern void fnDeleteNodeDataRef(IntPtr nodeDataHandle);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern float fnGetNodeDataFloat(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern void fnSetNodeDataFloat(IntPtr nodeData, int variableIndex, float value);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern int fnGetNodeDataInt(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern void fnSetNodeDataInt(IntPtr nodeData, int variableIndex, int value);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern int fnGetNodeDataEnum(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern void fnSetNodeDataEnum(IntPtr nodeData, int variableIndex, int value);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnGetNodeDataNodeLookup(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnSetNodeDataNodeLookup(IntPtr nodeData, int variableIndex, IntPtr nodeLookup);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnGetNodeDataHybridNodeLookup(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnSetNodeDataHybridNodeLookup(IntPtr nodeData, int variableIndex, IntPtr nodeLookup);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern float fnGetNodeDataHybridFloat(IntPtr nodeData, int variableIndex);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern float fnSetNodeDataHybridFloat(IntPtr nodeData, int variableIndex, float value);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern void fnAddNodeData(IntPtr listHandle, IntPtr nodeDataHandle);

    [DllImport(FastNoise.NATIVE_LIB)]
    public static extern IntPtr fnGetEncodedNodeTree(IntPtr nodeData);

    // [DllImport(FastNoiseBindings.NATIVE_LIB)]
    // public static extern IntPtr fnGetRootNodeDataFromEncodedNodeTree([MarshalAs(UnmanagedType.LPStr)] string encodedNodeTree);
}