using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Linq;

using UnityEditor;
using XNodeEditor;


namespace VoxelDataGeneratorGraph
{
    public class FastNoiseNode : ProceduralWorldGraphNodeBase
    {
        public bool isRootNode
        {
            get { return HasNoOutputs(); }
        }

        public bool HasNoOutputs()
        {
            foreach(var output in Outputs.Where(o => o.ValueType == typeof(FastNoiseTree)))
            {
                if(output.Connection == null) continue;
                if(output.Connection.node is FastNoiseNode) return false;
            }

            return true;
        }

        [HideInInspector] public bool isVisible = true;

        public void ToggleNodeVisibility(bool value)
        {
            isVisible = value;
        }

        [HideInInspector] public int metadataId;
        [NonSerialized] public Texture2D previewTexture;

        [ContextMenu("Copy Encoded Node Tree")]
        public void CopyEncodedNodeTree()
        {
            EditorGUIUtility.systemCopyBuffer = output.encodedNodeTree;
        }
        
        public override void ClearDynamicPorts()
        {
            return;
        }
        
        ~FastNoiseNode()
        {
            if(_nodeDataHandle != IntPtr.Zero)
            {
                throw new InvalidOperationException("Node data handle is not null. Did you forget to call CleanupDeserializedNodes()?");
            }
            if(_nodesListPtrHandle != IntPtr.Zero)
            {
                throw new InvalidOperationException("Node list handle is not null. Did you forget to call CleanupDeserializedNodes()?");
            }
        }
        
        [Output(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Override)] public FastNoiseTree output;

        public FastNoiseNode outputNode
        {
            get
            {
                NodePort port = GetOutputPort("output");
                if(port == null) return null;
                if(port.Connection == null) return null;
                return port.Connection.node as FastNoiseNode;
            }
        }

        public Metadata metadata
        {
            get { return FastNoise.nodeMetadata[metadataId]; }
        }
        public Metadata.Member[] members
        {
            get { return metadata.members.Values.ToArray(); }
        }
        
        public IntPtr _nodesListPtrHandle = IntPtr.Zero;
        public IntPtr _nodeDataHandle = IntPtr.Zero;

        public static FastNoiseNode CreateFastNoiseNodeInstance(string metadataName)
        {
            FastNoiseNode node = CreateInstance<FastNoiseNode>();
            node.InitializeFromMetadata(metadataName);
            return node;
        }

        public void InitializeFromMetadata(string metadataName)
        {
            metadataId = FastNoise.GetMetadataID(metadataName);
            InitializeFromMetadataID();
        }

        public void InitializeFromMetadataID()
        {
            _nodesListPtrHandle = FastNoiseBindings.fnGetNodeDataVectorHandleFromMetadata(metadataId);
            _nodeDataHandle = FastNoiseBindings.fnGetRootNodeDataFromVector(_nodesListPtrHandle);
        }

        public void InitializeFromPtr(IntPtr handle)
        {
            _nodeDataHandle = handle;
        }

        public override object GetValue(NodePort port)
        {
            if(port.fieldName == "output")
            {
                return output.encodedNodeTree;
            }
            else return null;
            
        }
        
#region Serialization

        public void SerializeRootNode()
        {
            if(!isRootNode) throw new InvalidOperationException("SerializeRootNode called on child node.");
            if(_nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException();
            
            CreateNodeInputDictionary();
            UpdateEncodedNodeTreeString();
            SerializeChildren();
        }

        private void SerializeChildren()
        {
            GetChildNodes().ForEach(node => {
                if(node != null)
                {
                    node.UpdateEncodedNodeTreeString();
                }
            });
        }

        public void UpdateEncodedNodeTreeString()
        {
            // if null, should be empty, no error in case of blend or something
            if(_nodeDataHandle == IntPtr.Zero)
            {
                Debug.LogWarning($"Serializing empty node tree for type {metadata.name}");
                output.encodedNodeTree = String.Empty;
                return;
            }

            output.encodedNodeTree = FastNoiseBindings.GetEncodedNodeDataTree(_nodeDataHandle);

            if(_nodeDataHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Node data handle is null after serialization.");
            }
            // Debug.Log($"serialized root node tree into {encodedNodeTree}");
        }
                
        public void DeserializeEntireTree()
        {
            // if(!isRootNode) throw new InvalidOperationException("Can only deserialize root node.");
            if(_nodesListPtrHandle != IntPtr.Zero) throw new InvalidOperationException();
            if(_nodeDataHandle != IntPtr.Zero) throw new InvalidOperationException();

            if (String.IsNullOrEmpty(output.encodedNodeTree))
            {
                InitializeFromMetadataID();
                SetupDependenciesOnEmptyParent();
                return;
            }
            
            _nodesListPtrHandle = FastNoiseBindings.fnDeserializeNodeDataVector(output.encodedNodeTree);
            if (_nodesListPtrHandle == IntPtr.Zero) throw new ExternalException("Terrain graph deserialization failure.");

            _nodeDataHandle = FastNoiseBindings.fnGetRootNodeDataFromVector(_nodesListPtrHandle);
            if (_nodeDataHandle == IntPtr.Zero) throw new ExternalException("Terrain graph deserialization failure.");

            SetupDependencyNodes();
        }

        private void SetupDependenciesOnEmptyParent()
        {
            if(_nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException();
            CreateNodeInputDictionary();
            
            foreach(var item in metadata.members)
            {
                Metadata.Member member = item.Value;
                if(member.type != Metadata.Member.VariableType.NodeLookup && member.type != Metadata.Member.VariableType.Hybrid) continue;

                NodePort port = null;

                if(member.type == Metadata.Member.VariableType.NodeLookup)
                {
                    port = inputsForNodeLookups[member.index];
                }
                if(member.type == Metadata.Member.VariableType.Hybrid)
                {
                    port = inputsForHybridNodes[member.index];
                }

                if(port.direction != NodePort.IO.Input) throw new InvalidOperationException("Input port not input");
                if(port.Connection == null) continue;
                
                FastNoiseNode childNode = port.Connection.node as FastNoiseNode;

                if(String.IsNullOrEmpty(childNode.output.encodedNodeTree))
                {
                    // note: change this to check if it has a string for nodetree to avoid graph saving surprises
                    childNode.InitializeFromMetadataID();
                    childNode.SetupDependenciesOnEmptyParent();
                }
                else
                {
                    childNode.DeserializeEntireTree();
                }
            }
        }

        private void SetupDependencyNodes()
        {
            if(_nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException();
            CreateNodeInputDictionary();
            
            foreach(var item in metadata.members)
            {
                Metadata.Member member = item.Value;

                IntPtr ptr = IntPtr.Zero;
                NodePort port = null;

                if(member.type == Metadata.Member.VariableType.NodeLookup)
                {
                    ptr = FastNoiseBindings.fnGetNodeDataNodeLookup(_nodeDataHandle, member.index);
                    port = inputsForNodeLookups[member.index];
                }
                else if(member.type == Metadata.Member.VariableType.Hybrid)
                {
                    ptr = FastNoiseBindings.fnGetNodeDataHybridNodeLookup(_nodeDataHandle, member.index);
                    port = inputsForHybridNodes[member.index];
                }
                else continue;

                if(port.direction != NodePort.IO.Input) throw new InvalidOperationException("Input port not input");
                if(port.Connection == null)
                {
                    // here we should either remove the node, or create the corresponding scriptable object
                    // we don't actually have to remove the node, but 
                    if(ptr != IntPtr.Zero)
                    {
                        // the ptr is a node lookup on something but it's missing the actual connection scriptable object
                        // so set it null
                        if(member.type == Metadata.Member.VariableType.NodeLookup)
                        {
                            FastNoiseBindings.fnSetNodeDataNodeLookup(_nodeDataHandle, member.index, IntPtr.Zero);
                        }
                        else
                        {
                            FastNoiseBindings.fnSetNodeDataHybridNodeLookup(_nodeDataHandle, member.index, IntPtr.Zero);
                        }

                        Debug.LogWarning($"Removing missing nodelookup on {metadata.name}, member {member.name}");
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    // the pointer doesn't have this on here, but the scriptableobject exists
                    // maybe we should just delete the scriptable object??
                    if(ptr == IntPtr.Zero)
                    {
                        // throw new InvalidOperationException();
                        FastNoiseNode node = port.Connection.node as FastNoiseNode;

                        if(String.IsNullOrEmpty(node.output.encodedNodeTree))
                        {
                            node.InitializeFromMetadataID();
                            node.SetupDependenciesOnEmptyParent();
                        }
                        else
                        {
                            node.DeserializeEntireTree();
                        }
                        continue;
                    }
                    else
                    {
                        FastNoiseNode childNode = port.Connection.node as FastNoiseNode;
                        childNode.InitializeFromPtr(ptr);
                        childNode.SetupDependencyNodes();
                    }
                }
            }            
        }

        public void CleanupDeserializedNodes()
        {
            _nodeDataHandle = IntPtr.Zero;

            if (_nodesListPtrHandle != IntPtr.Zero)
            {
                FastNoiseBindings.fnDeleteNodeDataVector(_nodesListPtrHandle);
                _nodesListPtrHandle = IntPtr.Zero;
            }
        }

        public List<FastNoiseNode> GetChildNodes()
        {
            List<NodePort> ports = Inputs.Where(p => p.ValueType == typeof(FastNoiseTree)).ToList();

            return ports.Select(p => {
                if(p.Connection != null)
                {
                    return p.Connection.node as FastNoiseNode;
                }
                else
                {
                    return null;
                }
            }).ToList();
        }
#endregion

        private bool _inputsCreated = false;

        private void CreateNodeInputs()
        {
            if(_inputsCreated) return;
            members.Where(m => m.type == Metadata.Member.VariableType.NodeLookup || m.type == Metadata.Member.VariableType.Hybrid).ToList().ForEach(member =>
            {
                NodePort nodeLookupPort = GetInputPort(member.name);
                // if(nodeLookupPort != null) throw new InvalidOperationException("Input ports exist!!");
                // throw new InvalidOperationException("Input ports exist!!");
                if(nodeLookupPort == null) AddDynamicInput(typeof(FastNoiseTree), connectionType: Node.ConnectionType.Override, fieldName: member.name);
            });

            _inputsCreated = true;
        }

        private void CreateNodeInputDictionary()
        {
            if(!_inputsCreated)
            {
                // might cause crash or problems
                CreateNodeInputs();
            }

            members.Where(m => m.type == Metadata.Member.VariableType.NodeLookup || m.type == Metadata.Member.VariableType.Hybrid).ToList().ForEach(member =>
            {
                NodePort nodeLookupPort = GetInputPort(member.name);

                if(nodeLookupPort == null) throw new InvalidOperationException("Missing input ports");

                if(member.type == Metadata.Member.VariableType.NodeLookup)
                {
                    if(!inputsForNodeLookups.TryGetValue(member.index, out NodePort port))
                    {
                        inputsForNodeLookups.Add(member.index, nodeLookupPort);
                    }
                }
                else
                {
                    if(!inputsForHybridNodes.TryGetValue(member.index, out NodePort port))
                    {
                        inputsForHybridNodes.Add(member.index, nodeLookupPort);
                    }
                }
            });
        }

        public void DrawNodeProperties()
        {
            ProceduralWorldGraph graph = this.graph as ProceduralWorldGraph;
            
            if(!graph.loadedGraph)
            {
                graph.LoadGraph();
            }

            if(_nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException($"Node data handle is zero for node {metadata.name}");

            CreateNodeInputs();
            CreateNodeInputDictionary();
            members.ToList().ForEach(member => DrawMember(member));
        }

        private void DrawMember(Metadata.Member member)
        {
            switch(member.type)
            {
                case Metadata.Member.VariableType.Float:
                    DrawFloat(member);
                    break;
                case Metadata.Member.VariableType.Int:
                    DrawInt(member);
                    break;
                case Metadata.Member.VariableType.Enum:
                    DrawEnum(member);
                    break;
                case Metadata.Member.VariableType.Hybrid:
                    DrawHybrid(member);
                    break;
                case Metadata.Member.VariableType.NodeLookup:
                    DrawNodeLookup(member);
                    break;
            }
        }

        private void DrawFloat(Metadata.Member member)
        {
            float floatValue = FastNoiseBindings.fnGetNodeDataFloat(_nodeDataHandle, member.index);
            float newFloatValue = UnityEditor.EditorGUILayout.FloatField(member.name, floatValue);
    
            // use epsilon to check for change
            if(Mathf.Abs(newFloatValue - floatValue) > Mathf.Epsilon)
            {
                FastNoiseBindings.fnSetNodeDataFloat(_nodeDataHandle, member.index, newFloatValue);
            }
        }

        private void DrawInt(Metadata.Member member)
        {
            int intValue = FastNoiseBindings.fnGetNodeDataInt(_nodeDataHandle, member.index);

            int newIntValue = UnityEditor.EditorGUILayout.IntField(member.name, intValue);

            if(newIntValue != intValue)
            {
                FastNoiseBindings.fnSetNodeDataInt(_nodeDataHandle, member.index, newIntValue);
            }
        }
        private void DrawEnum(Metadata.Member member)
        {
            int enumValue = FastNoiseBindings.fnGetNodeDataEnum(_nodeDataHandle, member.index);

            string[] choices = member.enumNames.Keys.ToArray();
            int newEnumValue = EditorGUILayout.Popup(enumValue, choices);

            if(newEnumValue != enumValue)
            {
                FastNoiseBindings.fnSetNodeDataEnum(_nodeDataHandle, member.index, newEnumValue);
            }
        }

        private void DrawNodeLookup(Metadata.Member member)
        {
            NodePort nodeLookupPort = inputsForNodeLookups[member.index];

            if(nodeLookupPort.IsConnected)
            {
                FastNoiseNode nodeLookup = nodeLookupPort.Connection.node as FastNoiseNode;
                if(nodeLookup._nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException($"Input node is null for member {member.name} on node {metadata.name}");
                if(_nodeDataHandle == IntPtr.Zero) throw new ExternalException("Parent node to input is null, but input is connected");

                FastNoiseBindings.fnSetNodeDataNodeLookup(_nodeDataHandle, member.index, nodeLookup._nodeDataHandle);
            }
            else
            {
                if(_nodeDataHandle == IntPtr.Zero) throw new ExternalException("Parent node to input is null, but input is connected");
                FastNoiseBindings.fnSetNodeDataNodeLookup(_nodeDataHandle, member.index, IntPtr.Zero);
            }
        }

        private Dictionary<int, NodePort> inputsForNodeLookups = new Dictionary<int, NodePort>();
        private Dictionary<int, NodePort> inputsForHybridNodes = new Dictionary<int, NodePort>();

        private void DrawHybrid(Metadata.Member member)
        {
            NodePort nodeLookupPort = inputsForHybridNodes[member.index];

            if(nodeLookupPort.IsConnected)
            {
                FastNoiseNode nodeLookup = nodeLookupPort.Connection.node as FastNoiseNode;
                if(nodeLookup._nodeDataHandle == IntPtr.Zero) throw new InvalidOperationException("Input node is null");
                FastNoiseBindings.fnSetNodeDataHybridNodeLookup(_nodeDataHandle, member.index, nodeLookup._nodeDataHandle);
            }
            else
            {
                FastNoiseBindings.fnSetNodeDataHybridNodeLookup(_nodeDataHandle, member.index, IntPtr.Zero);

                float hybridFloatValue = FastNoiseBindings.fnGetNodeDataHybridFloat(_nodeDataHandle, member.index);
                float newHybridFloatValue = UnityEditor.EditorGUILayout.FloatField(member.name, hybridFloatValue);

                if(newHybridFloatValue != hybridFloatValue)
                {
                    FastNoiseBindings.fnSetNodeDataHybridFloat(_nodeDataHandle, member.index, newHybridFloatValue);
                }
            }
        }

        public void ReinitializePreviewTextures()
        {
            ProceduralWorldGraph proceduralWorldGraph = graph as ProceduralWorldGraph;
            int resolution = proceduralWorldGraph.TexturePreviewResolution;

            GetPreviewTexture().Reinitialize(resolution, resolution);

            regenerateTextures = true;
        }

        private Texture2D GetPreviewTexture()
        {
            if(previewTexture == null)
            {
                ProceduralWorldGraph terrainGraph = graph as ProceduralWorldGraph;

                previewTexture = new Texture2D(terrainGraph.TexturePreviewResolution, terrainGraph.TexturePreviewResolution, TextureFormat.RGBAFloat, false);
                previewTexture.filterMode = FilterMode.Bilinear;
                previewTexture.wrapMode = TextureWrapMode.Clamp;
                previewTexture.name = "Preview Texture";
                previewTexture.hideFlags = HideFlags.DontSave;
            }

            return previewTexture;
        }

        private string _lastEncodedNodeTree;
        [NonSerialized] public bool regenerateTextures = true;

        public void DrawPreviewImage(int width)
        {
            ProceduralWorldGraph terrainGraph = graph as ProceduralWorldGraph;
            if(!terrainGraph.enablePreviewTextures) return;

            UpdatePreviewImage(width);

            width -= 32;
            // int width = GetWidth() - 32;
            float maxTextureSize = EditorGUIUtility.currentViewWidth - 32; // Calculate the maximum texture size based on available view width
            Rect rect = GUILayoutUtility.GetRect(width, width, GUILayout.ExpandWidth(true), GUILayout.Height(width));
            rect.position += new Vector2((rect.width - width) * 0.5f, (rect.height - width) * 0.5f); // Center the texture in the box
            rect.size = new Vector2(width, width); // Set the rect size to match the scaled texture size

            GUI.DrawTexture(rect, GetPreviewTexture(), ScaleMode.ScaleToFit);
        }

        private void UpdatePreviewImage(int width)
        {
            ProceduralWorldGraph terrainGraph = graph as ProceduralWorldGraph;

            bool shouldRegenerate = !String.Equals(_lastEncodedNodeTree, output.encodedNodeTree) || regenerateTextures;

            if(shouldRegenerate)
            {
                _lastEncodedNodeTree = output.encodedNodeTree;
                regenerateTextures = false;
            }
            else return;

            // if(!HasRequiredInputs()) return;
            
            int res = terrainGraph.TexturePreviewResolution;
            
            NativeArray<float> noise = new NativeArray<float>(res * res, Allocator.Temp);
            OutputMinMax minmax = default;

            bool failedToPreview = String.IsNullOrEmpty(output.encodedNodeTree);

            if(!failedToPreview)
            {
                using(var fn = new NativeFastNoise(Allocator.Temp, output.encodedNodeTree))
                {
                    minmax = fn.FillNativeArrayUniformGrid2D(noise, 0, 0, res, res, terrainGraph.Frequency, terrainGraph.DefaultSeed);
                }
            }
            
            // based on the noise array, fill the texture
            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    float value = failedToPreview ? 0 : noise[x * res + y];
                    // use minmax to turn value into a 0-1 range
                    value = failedToPreview ? 0 : (value - minmax.min) / (minmax.max - minmax.min);
                    GetPreviewTexture().SetPixel(x, y, failedToPreview ? Color.black : new Color(value, value, value));
                }
            }
            
            GetPreviewTexture().Apply();

            noise.Dispose();
        }

        public override NodeType GetNodeType() => NodeType.UIOnly;

        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData) { }
    }

#if UNITY_EDITOR
    [CustomNodeEditor(typeof(FastNoiseNode))]
    public class FastNoiseNodeEditor: NodeEditor
    {
        const int defaultSeed = 123;
        const int previewSize = 64;
        const float defaultFrequency = 1;
        
        public override void OnHeaderGUI()
        {
            FastNoiseNode node = target as FastNoiseNode;
            if(!node.isVisible) return;

            GUILayout.Label(target.name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        }

        public override string GetHeaderTooltip()
        {
            FastNoiseNode node = target as FastNoiseNode;
            return node.metadata.description;
        }
        
        private bool _nodeWasVisible = true;
        public override void OnBodyGUI()
        {
            FastNoiseNode node = target as FastNoiseNode;
            if(!node.isVisible)
            {
                return;
            }
            base.OnBodyGUI();
            node.DrawNodeProperties();
            node.DrawPreviewImage(GetWidth());
        }
        
        public override int GetWidth()
        {
            FastNoiseNode node = target as FastNoiseNode;
            if(!node.isVisible) return 0;

            return base.GetWidth();
        }
    }
#endif
}
