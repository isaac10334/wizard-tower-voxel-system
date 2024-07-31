using System.Collections.Generic;
using Unity.Mathematics;
using VoxelSystem;

namespace VoxelDataGeneratorGraph
{
    public class Shortcut : ProceduralWorldGraphNodeBase
    {
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)]
        public UnityEngine.Color32 color;
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)]
        public uint material;

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            throw new System.NotImplementedException();
        }

        public override NodeType GetNodeType() => NodeType.Shortcut;
        public override object GetValue(XNode.NodePort port)
        {
            return null;
        }

        public override bool IsRootNode()
        {
            return false;
        }

        public struct Data
        {
            
        }
    }
}
