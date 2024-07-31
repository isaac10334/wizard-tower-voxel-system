using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using XNode;
using VoxelSystem;

namespace VoxelDataGeneratorGraph
{
    public class GreaterThanNode : ProceduralWorldGraphNodeBase
    {
        public float threshold;
        [Input(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)] public PCGInOut value;
        [Output(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)] public PCGInOut outputIfGreaterThan;
        [Output(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)] public PCGInOut outputIfLessOrEqual;
        public override object GetValue(NodePort port)
        {
            return null;
        }
        public override NodeType GetNodeType() => NodeType.GreaterThan;
        
        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            nodeData.float0 = threshold;
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(outputIfGreaterThan)));
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(outputIfLessOrEqual)));
        }

        public override bool IsRootNode()
        {
            return false;
        }
    }
}
