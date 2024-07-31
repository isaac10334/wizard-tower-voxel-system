using System;
using UnityEngine;
using XNode;
using System.Collections;
using System.Collections.Generic;
using VoxelSystem;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using System.Linq;
using UnityEditor;
using XNodeEditor;

namespace VoxelDataGeneratorGraph
{
    public class NoiseIterator3DNode : ProceduralWorldGraphNodeBase
    {
        [HideInInspector] public bool isExpanded = true;
        public float frequency = 0.02f;
        public int seed = 123;
        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        public FastNoiseTree fastNoiseTree;
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public PCGInOut noiseValue;
        public override NodeType GetNodeType()
        {
            return NodeType.SampleNoise3D;
        }
        
        public override object GetValue(NodePort port)
        {
            return null;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            nodeData.float0 = frequency;
            nodeData.int0 = seed;
            nodeData.stringData = GetInputValue<string>(nameof(fastNoiseTree));
            nodeData.outputs.Add(PCGGraphHelper.GetNodeIDFromPort(lookup, this, nameof(noiseValue)));
        }
        
        public override bool IsRootNode()
        {
            return true;
        }
        
        public void ToggleAllInputNodes(bool toggle)
        {
            Stack<FastNoiseNode> nodeStack = new Stack<FastNoiseNode>();
            HashSet<FastNoiseNode> visitedNodes = new HashSet<FastNoiseNode>();

            // Directly add the inputs of the root node to the stack
            foreach (NodePort inputPort in this.Inputs)
            {
                FastNoiseNode inputNode = inputPort.Connection?.node as FastNoiseNode;

                if (inputNode != null)
                {
                    nodeStack.Push(inputNode);
                }
            }

            while (nodeStack.Count > 0)
            {
                FastNoiseNode currentNode = nodeStack.Pop();

                // If the current node is already visited, continue to the next iteration
                if (visitedNodes.Contains(currentNode))
                {
                    continue;
                }

                // Mark the current node as visited
                visitedNodes.Add(currentNode);

                // Toggle the current node based on the input value
                currentNode.ToggleNodeVisibility(toggle);

                // Iterate through all inputs of the current node
                foreach (NodePort inputPort in currentNode.Inputs)
                {
                    FastNoiseNode inputNode = inputPort.Connection?.node as FastNoiseNode;

                    if (inputNode != null && !visitedNodes.Contains(inputNode))
                    {
                        nodeStack.Push(inputNode);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomNodeEditor(typeof(NoiseIterator3DNode))]
    public class NoiseSampler3DNodeEditor: NodeEditor
    { 
        private NoiseIterator3DNode _node
        {
            get
            {
                return target as NoiseIterator3DNode;
            }
        }
        public override string GetHeaderTooltip()
        {
            return "Samples a FastNoise tree in 3D and calls the next node at every location.";
        }
        public override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
        }
        public override void OnBodyGUI()
        {
            string buttonLabel = _node.isExpanded ? "Collapse" : "Expand";
            
            if(_node.isExpanded)
            {
               NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("fastNoiseTree"));
            }
            else
            {
                GUILayout.Label("Expand to view nodes.");
            }
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("noiseValue"));

            if (GUILayout.Button(buttonLabel))
            {
                _node.isExpanded = !_node.isExpanded;

                var noiseSampler3D = (NoiseIterator3DNode)target;
                noiseSampler3D.ToggleAllInputNodes(_node.isExpanded);
            }
        }
    }
#endif
}
