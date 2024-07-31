using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using CsharpVoxReader;
using VoxelSystem.PCG;

namespace VoxelDataGeneratorGraph
{
    public class NodeData
    {
        public List<int> outputs;
        public float float0;
        public int int0;
        public int int1;
        public uint uint0;
        public string stringData;

        public NodeData()
        {
            outputs = new List<int>();
        }
    }

    public unsafe struct UnsafeNodeData
    {
        public NodeType Type;
        public bool IsRootNode;
        public int* outputs;
        public int outuptsLength;
        public int int0;
        public int int1;
        public float float0;
        public uint uint0;
        public FixedString512Bytes stringData512;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct NodeInstance
    {
        [FieldOffset(0)] public int arrayIndex;
        [FieldOffset(4)] public int intValue;
        [FieldOffset(4)] public float floatValue;
        [FieldOffset(4)] public int3 int3Value;
        [FieldOffset(4)] public int2 int2Value;
        [FieldOffset(24)] public int nodeId;
    }

    public unsafe struct GraphGeneratorJob : IVoxelDataGenerator
    {
        public NativeList<UnsafeNodeData> nodes;
        private bool _initialized;
        private Allocator _allocatorHandle;
        private MagicaVoxelObjectCollection _magicaVoxelObjectCollection;
        private int4 _key;
        [NativeDisableUnsafePtrRestriction] private uint* _colorsPtr;
        private NativeArray<bool> _visited;
        
        [BurstDiscard]
        public void BuildDataModel(Allocator allocator, ProceduralWorldGraph graph)
        {
            nodes = new NativeList<UnsafeNodeData>(32768, allocator);
            _magicaVoxelObjectCollection = MagicaVoxelObjectCollection.Create(0, Allocator.Persistent);

            _initialized = true;
            _allocatorHandle = allocator;

            List<ProceduralWorldGraphNodeBase> allNodes = graph.nodes.OfType<ProceduralWorldGraphNodeBase>().ToList();
            List<ProceduralWorldGraphNodeBase> nodesToKeep = new List<ProceduralWorldGraphNodeBase>();

            if (allNodes.Count == 0) return;
            Dictionary<ProceduralWorldGraphNodeBase, int> lookup = new Dictionary<ProceduralWorldGraphNodeBase, int>();
            Dictionary<MagicaVoxelFile, int> _magicaVoxelFileIDMap = new Dictionary<MagicaVoxelFile, int>();

            int index = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                ProceduralWorldGraphNodeBase node = allNodes[i];

                UnsafeNodeData pcgNode = new UnsafeNodeData
                {
                    Type = node.GetNodeType()
                };

                if (pcgNode.Type == NodeType.UIOnly) continue;

                pcgNode.IsRootNode = node.IsRootNode();
                nodes.Add(pcgNode);
                nodesToKeep.Add(node);

                // Map UINode to index
                lookup[node] = index;

                index++;
            }

            // Second loop to setup node data
            for (int i = 0; i < nodesToKeep.Count; i++)
            {
                ProceduralWorldGraphNodeBase node = nodesToKeep[i];

                UnsafeNodeData existing = nodes[i];

                NodeData nodeData = new NodeData();
                node.SetupNodeData(lookup, nodeData);

                // TODO - this is bad obviously
                existing.float0 = nodeData.float0;
                existing.int0 = nodeData.int0;
                existing.int1 = nodeData.int1;
                existing.uint0 = nodeData.uint0;

                if (!string.IsNullOrEmpty(nodeData.stringData))
                {
                    existing.stringData512 = nodeData.stringData;
                }

                //  set up outputs pointer from list
                existing.outuptsLength = nodeData.outputs.Count;
                existing.outputs = (int*)UnsafeUtility.Malloc(sizeof(int) * existing.outuptsLength, 4, allocator);
                for (int j = 0; j < existing.outuptsLength; j++) existing.outputs[j] = nodeData.outputs[j];

                // Special case: MagicaVoxel nodes
                if (node.GetNodeType() == NodeType.MagicaVoxelObject)
                {
                    existing.int0 = _magicaVoxelObjectCollection.GetOrAdd(_magicaVoxelFileIDMap,
                        (node as MagicaVoxelObject).voxelFile);
                }

                nodes[i] = existing;
            }

            _visited = new NativeArray<bool>(nodes.Length, allocator);
        }

        [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ExecuteGraph(ChunkWorker.Data data)
        {
            _key = data.key;
            _colorsPtr = data.colors;

            if (nodes.Length == 0) return;

            if (!_initialized)
            {
                throw new InvalidOperationException(
                    $"Graph executor not initialized. Call {nameof(BuildDataModel)} first.");
            }

            ExecuteRootNodes(data);
            data.isChunkAir[0] = false;
        }

        private void SampleUniformPoints2D(ChunkWorker.Data data, UnsafeNodeData nodeData, NodeInstance instance)
        {
            if (nodeData.outuptsLength == 0) return;
            instance.nodeId = nodeData.outputs[0];

            float2 biggestCenter = new float2(float.MinValue, float.MinValue);
            float2 smallestCenter = new float2(float.MaxValue, float.MaxValue);

            int voxelsBetweenPoints = nodeData.int0;
            int CircleSeparationConstant = voxelsBetweenPoints;

            int pointRadius = nodeData.int1;

            float radius = pointRadius;

            int squareSize = 32;

            float2 SquareCenterPosition =
                new float2(data.key.x, data.key.y) + new float2(VoxelEnvironment.ChunkSize / 2);

            float realRadius = (float)ChunkMath.FindRadiusOfCircleContainingSquare(pointRadius);

            int squareHalfSize = 16;

            int minBoundX =
                (int)(Math.Floor((SquareCenterPosition.x - squareHalfSize - radius) / CircleSeparationConstant) *
                      CircleSeparationConstant);
            int maxBoundX =
                (int)(Math.Ceiling((SquareCenterPosition.x + squareHalfSize + radius) / CircleSeparationConstant) *
                      CircleSeparationConstant);
            int minBoundY =
                (int)(Math.Floor((SquareCenterPosition.y - squareHalfSize - radius) / CircleSeparationConstant) *
                      CircleSeparationConstant);
            int maxBoundY =
                (int)(Math.Ceiling((SquareCenterPosition.y + squareHalfSize + radius) / CircleSeparationConstant) *
                      CircleSeparationConstant);

            int totalPoints = 0;

            for (int x = minBoundX; x <= maxBoundX; x += CircleSeparationConstant)
            {
                for (int y = minBoundY; y <= maxBoundY; y += CircleSeparationConstant)
                {
                    float2 circleCenter = new float2(x, y);
                    // Check if Circle overlaps or touches the Square.
                    if (ChunkMath.IsCircleTouchingOrOverlappingSquare(circleCenter, squareSize, radius,
                            SquareCenterPosition))
                    {
                        instance.int3Value = new int3(x, 0, y);
                        ExecuteNode(ref data, ref instance);

                        // Debug.Log($"center: {circleCenter} squarelength: {LengthOfSquare} radius: {radius} squarecenterpos {SquareCenterPosition}");
                        // if(circleCenter.x > biggestCenter.x && circleCenter.y > biggestCenter.y)
                        // {
                        //     biggestCenter.x = circleCenter.x;
                        //     biggestCenter.y = circleCenter.y;
                        // }
                        // if(circleCenter.x < smallestCenter.x && circleCenter.y < smallestCenter.y) 
                        // {
                        //     smallestCenter.x = circleCenter.x;
                        //     smallestCenter.y = circleCenter.y;
                        // }

                        totalPoints++;
                    }
                }
            }
            // Debug.Log($"{totalPoints} points!");
            // Debug.Log(biggestCenter);
            // Debug.Log(smallestCenter);
        }

        // public static int2 GetFarthestGridCellNegative(int x, int y, int circleRadius, int gridCellSize)
        // {
        //     float2 cell = new float2(0, 0);

        //     while (true)
        //     {
        //         float nextX = cell.x - gridCellSize;
        //         float nextY = cell.y - gridCellSize;

        //         double distance = Math.Sqrt(Math.Pow(nextX - x, 2) + Math.Pow(nextY - y, 2));

        //         if (distance > circleRadius)
        //             break;

        //         cell.x = nextX;
        //         cell.y = nextY;
        //     }

        //     return (int2)cell;
        // }

        // public static int2 GetFarthestGridCell(int x, int y, int circleRadius, int gridCellSize)
        // {
        //     Debug.Log($"{x}, {y}")
        //     float2 cell = new float2(0, 0);

        //     while (true)
        //     {
        //         float nextX = cell.x + gridCellSize;
        //         float nextY = cell.y + gridCellSize;

        //         double distance = Math.Sqrt(Math.Pow(nextX - x, 2) + Math.Pow(nextY - y, 2));

        //         if (distance > circleRadius)
        //             break;

        //         cell.x = nextX;
        //         cell.y = nextY;
        //     }

        //     return (int2)cell;
        // }

        private void HandleMagicaVoxelNode(ChunkWorker.Data data, int3 positionToPlaceAt, UnsafeNodeData nodeData)
        {
            // Handle a MagicaVoxelNode. These run per sample output. 
            int id = nodeData.int0;

            // TODO cache this, what is this?
            NativeArray<Chunk16> chunks = _magicaVoxelObjectCollection.GetVoxelDataArray(id, Allocator.Temp);

            // loop all sub-chunks of the MV object and find the spot on the chunk that the voxel goes
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk16 inputChunk = chunks[i];
                int3 thisChunkWorldPosition = positionToPlaceAt + inputChunk.position;
                int4 mvChunkCube = new int4(thisChunkWorldPosition, 16);

                int4 outputChunkCube = new int4(data.key.xyz, data.key.w * VoxelEnvironment.ChunkSize);
                bool intersects = ChunkMath.DoCubesIntersect(mvChunkCube, outputChunkCube);
                if (!intersects) continue;


                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            int3 localChunk16Position = new int3(x, y, z);
                            int localChunk16Index = ChunkMath.FlattenIndex(localChunk16Position, 16);

                            uint color = inputChunk.colorsPtr[localChunk16Index];
                            if (color == 0) continue;

                            int3 positionOnMagicaVoxelObject = inputChunk.position + localChunk16Position;

                            int3 positionInWorld = positionToPlaceAt + positionOnMagicaVoxelObject;

                            if (!ChunkMath.IsInsideCube(positionInWorld, outputChunkCube)) continue;

                            // get the local voxel inside the thing
                            // confused - shouldn't we just check if this is within bounds instead of above?
                            int3 positionInOutputChunk = positionInWorld - data.key.xyz;
                            positionInOutputChunk /= data.key.w;

                            int indexInOutputChunk = ChunkMath.FlattenIndex(positionInOutputChunk);

                            if (indexInOutputChunk < 0 || indexInOutputChunk >= VoxelEnvironment.ChunkSizeCubed)
                                continue;

                            _colorsPtr[indexInOutputChunk] = color;
                        }
                    }
                }
            }
        }

        #region Helpers

        private void ExecuteRootNodes(ChunkWorker.Data data)
        {
            NodeInstance instance = default;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsRootNode)
                {
                    instance.nodeId = i;
                    ExecuteRootNode(data, instance);
                }
            }
        }

        [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteRootNode(ChunkWorker.Data data, NodeInstance instance)
        {
            if (instance.nodeId == -1) return;
            _visited[instance.nodeId] = true;

            UnsafeNodeData nodeData = nodes[instance.nodeId];
            NodeType type = nodeData.Type;

            switch (type)
            {
                case NodeType.SampleNoise3D:
                {
                    if (nodeData.outputs[0] == -1) break;

                    FixedString512Bytes tree = nodeData.stringData512;
                    float frequency = nodeData.float0;
                    int seed = nodeData.int0;

                    NativeArray<float> noise = new NativeArray<float>(VoxelEnvironment.ChunkSizeCubed, Allocator.Temp);
                    NativeFastNoise.GenUniformGrid3D(tree, noise, data.key, frequency, seed);

                    for (int i = 0; i < VoxelEnvironment.ChunkSizeCubed; i++)
                    {
                        // we can reuse instance
                        instance.nodeId = nodeData.outputs[0];
                        instance.floatValue = noise[i];
                        instance.arrayIndex = i;
                        ExecuteNode(ref data, ref instance);
                    }

                    break;
                }
                case NodeType.UniformPointSampler:
                {
                    SampleUniformPoints2D(data, nodeData, instance);
                    break;
                }
                default:
                    break;
            }
        }

        [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteNode(ref ChunkWorker.Data data, ref NodeInstance instance)
        {
            if (instance.nodeId == -1) return;
            _visited[instance.nodeId] = true;

            UnsafeNodeData nodeData = nodes[instance.nodeId];

            switch (nodeData.Type)
            {
                case NodeType.MagicaVoxelObject:
                {
                    HandleMagicaVoxelNode(data, instance.int3Value, nodeData);
                    break;
                }
                case NodeType.Color:
                {
                    uint color = nodeData.uint0;
                    SetColor(instance.arrayIndex, color);
                    break;
                }
                case NodeType.GreaterThan:
                {
                    float value = instance.floatValue;
                    float threshold = nodeData.float0;
                    int outputIfGreater = nodeData.outputs[0];
                    int outputIfLessOrEqual = nodeData.outputs[1];

                    instance.floatValue = value;

                    if (value > threshold)
                    {
                        instance.nodeId = outputIfGreater;
                    }
                    else
                    {
                        instance.nodeId = outputIfLessOrEqual;
                    }

                    ExecuteNode(ref data, ref instance);
                    break;
                }
                case NodeType.Random:
                {
                    if (nodeData.outuptsLength == 0) break;
                    throw new NotImplementedException();
                    // we need to build a random somehow
                    // we can't just create one from the seed because it needs to be reused throughout the lifecycle
                    // so we have to store them and somehow get them - maybe a hashmap based on node IDs
                    // range between outputs
                    // int output = nodeData.outputs[rand.NextInt(nodeData.outuptsLength)];
                    // instance.nodeId = output;
                    // ExecuteNode(ref data, ref instance);
                    break;
                }
                case NodeType.UIOnly:
                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetColor(int i, uint color)
        {
            _colorsPtr[i] = color;
        }

        #endregion

        public void Dispose()
        {
            foreach (UnsafeNodeData unsafeNodeData in nodes)
            {
                UnsafeUtility.Free(unsafeNodeData.outputs, _allocatorHandle);
            }

            _visited.Dispose();
            nodes.Dispose();
            _magicaVoxelObjectCollection.Dispose();
        }
    }
}