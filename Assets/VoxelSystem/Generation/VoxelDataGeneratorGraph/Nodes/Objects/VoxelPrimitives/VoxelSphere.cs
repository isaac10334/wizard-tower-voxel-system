using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelDataGeneratorGraph
{
    public class VoxelSphere : ProceduralWorldGraphNodeBase
    {
        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            throw new System.NotImplementedException();
        }

        // [Tooltip("Radius in voxels between points.")]
        // public int size = 10;
        // [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        // public int2 input;
        // [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        // public int2[] output;
        // public override NodeType GetNodeType() => NodeType.UniformPointSampler;

        // public override bool IsRootNode()
        // {
        //     return true;
        // }

        // public Data GetNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup)
        // {
        //     return new Data()
        //     {
        //         size = size,
        //         input = PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(input), true),
        //         output = PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(output), false),
        //     };
        // }

        // public struct Data
        // {
        //     public int size;
        //     public int input;
        //     public int output;
        // }
        public override NodeType GetNodeType()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsRootNode()
        {
            throw new System.NotImplementedException();
        }
    }
}
