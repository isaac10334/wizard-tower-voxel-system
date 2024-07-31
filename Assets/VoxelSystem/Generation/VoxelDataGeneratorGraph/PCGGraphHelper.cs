using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{
    public static class PCGGraphHelper
    {
        public static int GetNodeIDFromPort(Dictionary<ProceduralWorldGraphNodeBase, int> uiNodeToIndex, XNode.Node parentNode, string name)
        {

            ProceduralWorldGraphNodeBase node = GetPCGPort(parentNode, name);
            if(node == null) return -1;
            
            if(!uiNodeToIndex.TryGetValue(node, out int value)) return -1;
            return value;
        }
        public static ProceduralWorldGraphNodeBase GetPCGPort(XNode.Node node, string name)
        {
            NodePort port = node.GetOutputPort(name);
            if(port == null || port.Connection == null || port.Connection.node == null || !(port.Connection.node is ProceduralWorldGraphNodeBase)) return null;
            return port.Connection.node as ProceduralWorldGraphNodeBase;
        }
    }
}