using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelSystem
{
    public struct NativeLookupTableData
    {
        [ReadOnly] public NativeArray<int3> unflattenedIndices;
        [ReadOnly] public NativeArray<int3> indexComponents;
        [ReadOnly] public NeighborTables neighborsTable;
    }

    public static class LookupTables
    {
        public static NativeLookupTableData Data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Setup()
        {
            Data.unflattenedIndices = LookupTableMath.GenerateUnflattenedIndicesTable(Allocator.Domain);
            Data.indexComponents = LookupTableMath.GenerateIndexComponentsTable(Allocator.Domain);
            Data.neighborsTable = LookupTableMath.GenerateNeighborTables(Allocator.Domain);
        }
    }
}