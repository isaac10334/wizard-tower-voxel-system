using System;
using System.Collections.Generic;
using Unity.Mathematics;
using VoxelSystem;

namespace VoxelDataGeneratorGraph
{
    public class MagicaVoxelObject : ProceduralWorldGraphNodeBase
    {
        public MagicaVoxelFile voxelFile;

        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)] 
        public int2 samplePosition;
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public UnityEngine.Color32 color;
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public uint material;
        public override NodeType GetNodeType() => NodeType.MagicaVoxelObject;
        public override object GetValue(XNode.NodePort port)
        {
            return null;
        }

        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            // we need to set up RADIUS somehow
            // 
            // nothing actually needs to happen here
            // throw new NotImplementedException();
        }
    }
}
