using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Linq;
using UnityEditor;
using XNodeEditor;
using System.Data.Common;

namespace VoxelDataGeneratorGraph
{
    // This one requires dynamic outputs
    public class RandomNode : ProceduralWorldGraphNodeBase
    {
        [Input(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)] public PCGInOut value;
        public override object GetValue(NodePort port)
        {
            return null;
        }
        public override NodeType GetNodeType() => NodeType.Random;
        
        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            foreach(var output in Outputs)
            {
                int id = PCGGraphHelper.GetNodeIDFromPort(lookup, this, output.fieldName);
                nodeData.outputs.Add(id);
            }
        }
    }

#if UNITY_EDITOR
    [CustomNodeEditor(typeof(RandomNode))]
    public class RandomNodeEditor: NodeEditor
    {
        public override void OnBodyGUI()
        {
            RandomNode randomNode = target as RandomNode;
            base.OnBodyGUI();

            if(GUILayout.Button("Add Output"))
            {
                randomNode.AddDynamicOutput(typeof(PCGInOut), connectionType: Node.ConnectionType.Override, 
                    typeConstraint: Node.TypeConstraint.None,
                    fieldName: randomNode.Outputs.Count().ToString());
            }
            if(GUILayout.Button("Remove Output"))
            {   
                if(randomNode.Outputs.Count() != 0)
                {
                    randomNode.RemoveDynamicPort((randomNode.Outputs.Count() - 1).ToString());
                }
            }
        }
    }
}
#endif