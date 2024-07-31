using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{
    public class ColorNode : ProceduralWorldGraphNodeBase
    {
        public Color32 color;
        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)] public PCGInOut input;
        [Output(typeConstraint = TypeConstraint.None, connectionType = ConnectionType.Override)] public PCGInOut colorOutput;
        public override object GetValue(NodePort port)
        {
            return color;
        }
        
        public override NodeType GetNodeType() => NodeType.Color;
        
        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, "colorOutput"));
            nodeData.uint0 = VoxelSystem.VoxelColor32.RGBToUint(color.r, color.g, color.b);
        }
        
        public override bool IsRootNode() => false;
    }
}
