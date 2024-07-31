using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using XNode;

namespace VoxelDataGeneratorGraph
{
    [Serializable]
    public class PointNodeConnection {}

    public abstract class ProceduralWorldGraphNodeBase : XNode.Node
    {
        public abstract NodeType GetNodeType();
        public abstract bool IsRootNode();
        public abstract void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData);
    }
}
