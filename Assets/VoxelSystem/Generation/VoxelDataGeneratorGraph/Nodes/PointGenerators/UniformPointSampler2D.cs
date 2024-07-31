
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using XNode;

namespace VoxelDataGeneratorGraph
{
    public class UniformPointSampler2D : PointGenerator
    {
        [Tooltip("Radius in voxels between points.")]
        public int voxelsBetweenPoints = 10;
        public int radius = 10;
        public override NodeType GetNodeType() => NodeType.UniformPointSampler;

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public override bool IsRootNode()
        {
            return true;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            nodeData.int0 = voxelsBetweenPoints;
            nodeData.int1 = radius;
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(output)));
        }
    }
}
