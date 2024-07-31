using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{
    public class GetNoiseSurface : ProceduralWorldGraphNodeBase
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public int input;
        [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public float surface;
        public override object GetValue(NodePort port)
        {
            return null;
        }
        
        public override NodeType GetNodeType() => NodeType.Material;

        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            throw new System.NotImplementedException();
        }

        public struct Data
        {
            public uint MaterialId;
            public int Input;
            public int Output;
        }
    }
}
