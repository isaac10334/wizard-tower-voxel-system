using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{
    public class OverridePosition : ProceduralWorldGraphNodeBase
    {
        public uint materialId;

        [Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.None)]
        public int x;

        [Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.None)]
        public int y;

        [Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.None)]
        public int z;

        [Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.None)]
        public int3 input;

        [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)]
        public int2 output;

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public override NodeType GetNodeType() => NodeType.OverridePosition;

        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            throw new System.NotImplementedException();
        }
    }
}