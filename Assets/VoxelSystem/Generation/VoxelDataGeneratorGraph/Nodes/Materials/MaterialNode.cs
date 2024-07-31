using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{

    public class MaterialNode : ProceduralWorldGraphNodeBase
    {
        public uint materialId;
        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public int input;
        [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public uint output;
        public override object GetValue(NodePort port)
        {
            return null;
        }
        
        public override NodeType GetNodeType() => NodeType.Material;
        
        public unsafe override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            nodeData.uint0 = materialId;
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(output)));
        }

        public override bool IsRootNode()
        {
            return false;
        }
    }
}
