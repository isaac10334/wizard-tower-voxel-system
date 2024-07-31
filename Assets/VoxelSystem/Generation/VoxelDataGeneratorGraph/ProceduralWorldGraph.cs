using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using XNode;
using Node = XNode.Node;
using UnityEditor;
using XNodeEditor;
using Unity.Collections;
using Unity.Mathematics;
using VoxelSystem;
using Unity.Collections.LowLevel.Unsafe;

namespace VoxelDataGeneratorGraph
{
    // necessary for type switch statement - maybe a solution with function pointers or something would be better
    public enum NodeType
    { 
        UIOnly,
        UniformPointSampler,
        SampleNoise3D,
        MagicaVoxelObject,
        Color,
        Material,
        OverridePosition,
        Shortcut,
        GreaterThan,
        Random
    }
    
    [Serializable, CreateAssetMenu(fileName = "ProceduralWorldGraph", menuName = "VoxelSystem/ProceduralWorldGraph"), RequireNode(typeof(ProceduralWorldOutput))]
    public class ProceduralWorldGraph : NodeGraph
    {
        public string previewErrorMessage;
        public const int MaximumTexturePreviewResolution = 1024;
        public const int MinimumTexturePreviewResolution = 8;
        public NodeEditorPreferences.Settings settings;
        public int TexturePreviewResolution
        {
            get { return _texturePreviewResolution; }
            set
            {
                if(value < MinimumTexturePreviewResolution) value = MinimumTexturePreviewResolution;
                if(value > MaximumTexturePreviewResolution) value = MaximumTexturePreviewResolution;
                _texturePreviewResolution = value;

                ReinitializeNodePreviewTextures();
            }
        }
        private int _texturePreviewResolution = 128;

        public bool enablePreviewTextures = true;
        [NonSerialized] public bool loadedGraph = false;
        public float Frequency
        {
            get { return _frequency; }
            set 
            {
                _frequency = value;
                ReinitializeNodePreviewTextures();
            }
        }
        private float _frequency = 0.02f;
        public int DefaultSeed
        {
            get { return _defaultSeed; }
            set 
            {
                _defaultSeed = value;
                ReinitializeNodePreviewTextures();
            }
        }
        private int _defaultSeed = 123;
        public Material previewMaterial;
        public unsafe Mesh GenerateAndMeshPreviewPrefab(int4 key)
        {
            LookupTables lts = new LookupTables(Allocator.Temp);

            ChunkWorker chunkworker = null;
            try
            {
                Mesh mesh = new Mesh();

                Allocator allocator = Allocator.Persistent;
                
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                GraphGeneratorJob graphGeneratorExecutor = new GraphGeneratorJob();
                graphGeneratorExecutor.BuildDataModel(Allocator.Persistent, this);

                chunkworker = new ChunkWorker(lts, (worker) => {
                    worker.key = key;
                    worker.vertices = new NativeList<Vertex>(allocator);
                    worker.triangles = new NativeList<int>(allocator);
                    
                    worker.AllocateMeshDataArray();
                    
                    worker.isChunkAir = (bool*)UnsafeUtility.Malloc(1 * sizeof(bool), 4, allocator);
                    worker.isChunkAir[0] = false;

                    worker.AllocateColorsPtr(allocator);

                    worker.graphGeneratorJob = graphGeneratorExecutor;
                    worker.seed = _defaultSeed;

                    return worker;
                });

                sw.Start();
                chunkworker.RunGenerationJob();
                // chunkworker.ExecuteGenerationJobNoBurst();
                sw.Stop();

                if(!chunkworker.data.isChunkAir[0] && chunkworker.data.vertices.Length > 0)
                {
                    chunkworker.ApplyToUnityMesh(mesh);
                }

                chunkworker.Dispose();

                previewErrorMessage = "";
                
                lts.Dispose();
                return mesh;
            }
            catch(Exception e)
            {
                if(chunkworker != null)
                    chunkworker.Dispose();


                previewErrorMessage = e.Message;

                Debug.LogException(e);
            }

            lts.Dispose();
            return null;
        }
#if UNITY_EDITOR
        private void OnDisable()
        {
            SaveGraph();
        }
        public void SaveGraph()
        {
            if(!loadedGraph) LoadGraph();
            UpdateGraph();
            AssetDatabase.SaveAssets();
        }
        public void ReloadGraph()
        {
            UpdateGraph();
            LoadGraph();
        }
        public void UpdateGraph()
        {
            if(!loadedGraph) return;

            FindAllRootNodes().ForEach(node => {
                node.SerializeRootNode();
            });

            FreeAllNodes();
            
            loadedGraph = false;
        }

        public void LoadGraph()
        {
            FreeAllNodes();

            FindAllRootNodes().ForEach(rootNode => {
                rootNode.DeserializeEntireTree();
            });

            loadedGraph = true;
        }
        
        private void FreeAllNodes()
        {
            nodes.OfType<FastNoiseNode>().ToList().ForEach(node => node.CleanupDeserializedNodes());
        }

        public void ReinitializeNodePreviewTextures()
        {
            nodes.OfType<FastNoiseNode>().ToList().ForEach(node => node.ReinitializePreviewTextures());
        }
        
        public override Node AddNode(Type type)
        {
            if(type.IsAssignableFrom(typeof(FastNoiseNode))) return null;
            return base.AddNode(type);
        }
        
        public FastNoiseNode AddFastNoiseNodeFromMetadata(string metadataname)
        {
            Node.graphHotfix = this;
            FastNoiseNode node = FastNoiseNode.CreateFastNoiseNodeInstance(metadataname);
            node.graph = this;
            nodes.Add(node);
            return node;
        }
        public override Node CopyNode(Node node)
        {
            if(node is FastNoiseNode)
            {
                FastNoiseNode original = node as FastNoiseNode;
                return AddFastNoiseNodeFromMetadata(original.metadata.name);
            }
            
            return base.CopyNode(node);
        }
        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);
        }
        public override void Clear() { }
        public override XNode.NodeGraph Copy()
        {
            return base.Copy();
        }
        protected override void OnDestroy()
        {
            FreeAllNodes();
            base.OnDestroy();
        }
    #endif
        public string GetFirstRootNodeTree()
        {
            return FindAllRootNodes().FirstOrDefault().output.encodedNodeTree;
        }
        
        private List<FastNoiseNode> FindAllRootNodes() => nodes.OfType<FastNoiseNode>().Where(node => node.isRootNode).ToList();
        
        public UnityEngine.Material GetPreviewMaterial()
        {
            Shader s = Shader.Find("Shader Graphs/VoxelShader");
            if(s == null)
            {
                Debug.LogWarning($"Missing shader for preview material. Please manually assign the preview material.", this);
                return null;
            }
            return new Material(s);
        }
    }

#if UNITY_EDITOR
    [CustomNodeGraphEditor(typeof(ProceduralWorldGraph))]
    public class ProceduralWorldGraphEditor : NodeGraphEditor
    {
        private ProceduralWorldGraph _graph
        {
            get
            {
                return target as ProceduralWorldGraph;
            }
        }
        public override void OnGUI()
        {
            GUIContent[] contents = new GUIContent[2];
            contents[0] = EditorGUIUtility.IconContent("_Help");
            contents[0].tooltip = "Open Documentation";
            contents[1] = EditorGUIUtility.IconContent("d_winbtn_win_max");
            contents[1].tooltip = "Toggle Previews";

            GUILayout.BeginVertical();
            for (int i = 0; i < contents.Length; i++)
            {
                if (GUILayout.Button(contents[i], GUILayout.ExpandWidth(false)))
                {
                    if (i == 0)
                    {
                        SetupWindow.OpenDocumentation();
                    }
                    else if (i == 1)
                    {
                        _graph.enablePreviewTextures = !_graph.enablePreviewTextures;
                    }
                }
            }
            GUILayout.EndVertical();
            _graph.ReloadGraph();
            
            // Keep repainting the GUI of the active NodeEditorWindow
            NodeEditorWindow.current.Repaint();
        }
        
        public override void OnOpen()
        {
        }

        public override void OnWindowFocusLost()
        {
        }
        public override void OnWindowFocus()
        {

        }

		public override string GetNodeMenuName(System.Type type)
        {
            if(type == typeof(FastNoiseNode)) return null;
            return null;
		}

        public override int GetNodeMenuOrder(Type type)
        {
            return base.GetNodeMenuOrder(type);
        }
        
        /// <param name="menu"></param>
        /// <param name="compatibleType">Use it to filter only nodes with ports value type, compatible with this type</param>
        /// <param name="direction">Direction of the compatiblity</param>
        public override void AddContextMenuItems(GenericMenu menu, Type compatibleType, XNode.NodePort.IO direction = XNode.NodePort.IO.Input)
        {
            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            Metadata[] allMetadata = FastNoise.nodeMetadata;

            foreach(Metadata metadata in allMetadata)
            {
                menu.AddItem(new GUIContent($"Noise/{metadata.groupName}/{metadata.unformattedName}"), false, () => CreateFastNoiseNode(metadata, pos));
            }

            menu.AddItem(new GUIContent($"Noise Samplers/Noise Sampler 3D"), false, () => CreateNode(typeof(NoiseIterator3DNode), pos));
            // menu.AddItem(new GUIContent($"FastNoise/Noise Samplers/Noise Sampler 2D"), false, () => CreateNode(typeof(NoiseSampler2DNode), pos));
            
            // Point Generators
            menu.AddItem(new GUIContent($"Point Generators/Uniform"), false, () => CreateNode(typeof(UniformPointSampler2D), pos));
            
            // voxel objects
            menu.AddItem(new GUIContent($"Voxel Objects/MagicaVoxel Object"), false, () => CreateNode(typeof(MagicaVoxelObject), pos));
            menu.AddItem(new GUIContent($"Voxel Objects/Primitives/Cube"), false, () => CreateNode(typeof(VoxelCube), pos));
            menu.AddItem(new GUIContent($"Voxel Objects/Primitives/Sphere"), false, () => CreateNode(typeof(VoxelSphere), pos));
            menu.AddItem(new GUIContent($"Voxel Objects/Primitives/Cylinder"), false, () => CreateNode(typeof(VoxelCylinder), pos));

            // control
            menu.AddItem(new GUIContent($"Control/Greater Than"), false, () => CreateNode(typeof(GreaterThanNode), pos));
            menu.AddItem(new GUIContent($"Control/Random"), false, () => CreateNode(typeof(RandomNode), pos));
            
            // colors
            menu.AddItem(new GUIContent($"Colors/Color"), false, () => CreateNode(typeof(ColorNode), pos));
            
            // materials
            menu.AddItem(new GUIContent($"Materials/Material"), false, () => CreateNode(typeof(MaterialNode), pos));

            // modifiers
            menu.AddItem(new GUIContent($"Modifiers/Override Position"), false, () => CreateNode(typeof(OverridePosition), pos));

            // utilities
            menu.AddItem(new GUIContent($"Utility/Get Noise Surface"), false, () => CreateNode(typeof(GetNoiseSurface), pos));
            menu.AddItem(new GUIContent($"Utility/Shortcut"), false, () => CreateNode(typeof(Shortcut), pos));
            
            base.AddContextMenuItems(menu, compatibleType, direction);
        }
        
        /// <summary> Create a node and save it in the graph asset </summary>
        public override XNode.Node CreateNode(Type type, Vector2 position)
        {
            if(!typeof(XNode.Node).IsAssignableFrom(type)) throw new System.ArgumentException("Type must be a Node", "type");

            Undo.RecordObject(target, "Create Node");
            XNode.Node node = target.AddNode(type);
            if (node == null) return null; // handle null nodes to avoid nullref exceptions
            Undo.RegisterCreatedObjectUndo(node, "Create Node");
            node.position = position;
            if (node.name == null || node.name.Trim() == "") node.name = NodeEditorUtilities.NodeDefaultName(type);
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target))) AssetDatabase.AddObjectToAsset(node, target);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            NodeEditorWindow.RepaintAll();
            return node;
        }
        /// <summary> Create a node and save it in the graph asset </summary>
        public XNode.Node CreateFastNoiseNode(Metadata metadata, Vector2 position)
        {
            // Undo.RecordObject(target, "Create FastNoiseNode");
            
            ProceduralWorldGraph graph = target as ProceduralWorldGraph;
            graph.ReloadGraph();

            XNode.Node node = graph.AddFastNoiseNodeFromMetadata(metadata.name);
            graph.ReloadGraph();

            // Undo.RegisterCreatedObjectUndo(node, "Create FastNoiseNode");
            
            node.position = position;
            node.name = metadata.unformattedName;
            
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target))) AssetDatabase.AddObjectToAsset(node, target);

            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            NodeEditorWindow.RepaintAll();
            return node;
        }
        /// <summary> Creates a copy of the original node in the graph </summary>
        public override XNode.Node CopyNode(XNode.Node original)
        {
            if(original is FastNoiseNode)
            {
                XNode.Node fastNoiseNode = target.CopyNode(original);
                fastNoiseNode.name = original.name;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target))) AssetDatabase.AddObjectToAsset(fastNoiseNode, target);
                if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
                return fastNoiseNode; 
            }

            Undo.RecordObject(target, "Duplicate Node");
            XNode.Node node = target.CopyNode(original);
            Undo.RegisterCreatedObjectUndo(node, "Duplicate Node");
            node.name = original.name;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target))) AssetDatabase.AddObjectToAsset(node, target);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary> Return false for nodes that can't be removed </summary>
        public override bool CanRemove(XNode.Node node)
        {
            // Check graph attributes to see if this node is required
            Type graphType = target.GetType();
            XNode.NodeGraph.RequireNodeAttribute[] attribs = Array.ConvertAll(
                graphType.GetCustomAttributes(typeof(XNode.NodeGraph.RequireNodeAttribute), true), x => x as XNode.NodeGraph.RequireNodeAttribute);
            if (attribs.Any(x => x.Requires(node.GetType()))) {
                if (target.nodes.Count(x => x.GetType() == node.GetType()) <= 1) {
                    return false;
                }
            }
            return true;
        }

        /// <summary> Safely remove a node and all its connections. </summary>
        public override void RemoveNode(XNode.Node node)
        {
            if (!CanRemove(node)) return;
            if(node is FastNoiseNode)
            {
                OnRemoveFastNoiseNode(node as FastNoiseNode);
            }
            else
            {
                // Remove the node
                Undo.RecordObject(node, "Delete Node");
                Undo.RecordObject(target, "Delete Node");
                foreach (var port in node.Ports)
                    foreach (var conn in port.GetConnections())
                        Undo.RecordObject(conn.node, "Delete Node");
            }

            target.RemoveNode(node);
            Undo.DestroyObjectImmediate(node);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        private void OnRemoveFastNoiseNode(FastNoiseNode node)
        {
            ProceduralWorldGraph graph = target as ProceduralWorldGraph;
            graph.ReloadGraph();
        }

        /// <summary> Deal with objects dropped into the graph through DragAndDrop </summary>
        public override void OnDropObjects(UnityEngine.Object[] objects)
        {
            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            // just log types
            foreach (var obj in objects)
            {
                if(obj is VoxelSystem.MagicaVoxelFile)
                {
                    // create a MagicaVoxelObject node
                    MagicaVoxelObject mvNode = CreateNode(typeof(MagicaVoxelObject), pos) as MagicaVoxelObject;
                    mvNode.voxelFile = obj as VoxelSystem.MagicaVoxelFile;
                    return;
                }
                if(obj is VoxelSystem.VoxelMaterial)
                {
                    MaterialNode matNode = CreateNode(typeof(MaterialNode), pos) as MaterialNode;
                    matNode.materialId = MaterialsDatabase.GetDefaultMaterialsDatabase().GetIDFromMaterial(obj as VoxelSystem.VoxelMaterial);
                    return;
                }
            }

            base.OnDropObjects(objects);
        }
        public override Gradient GetNoodleGradient(XNode.NodePort output, XNode.NodePort input)
        {
            return base.GetNoodleGradient(output, input);
        }
        public override float GetNoodleThickness(XNode.NodePort output, XNode.NodePort input)
        {
            return base.GetNoodleThickness(output, input);
        }
        public override NoodlePath GetNoodlePath(XNode.NodePort output, XNode.NodePort input)
        {
            return base.GetNoodlePath(output, input);
        }
        public override NoodleStroke GetNoodleStroke(XNode.NodePort output, XNode.NodePort input)
        {
            return base.GetNoodleStroke(output, input);
        }
        public override Color GetPortColor(XNode.NodePort port)
        {
            return base.GetPortColor(port);
        }
        public override GUIStyle GetPortStyle(XNode.NodePort port)
        {
            return base.GetPortStyle(port);
        }
        public override Color GetPortBackgroundColor(XNode.NodePort port)
        {
            return base.GetPortBackgroundColor(port);
        }
        public override string GetPortTooltip(XNode.NodePort port)
        {
            return base.GetPortTooltip(port);
        }
    }
#endif
}
