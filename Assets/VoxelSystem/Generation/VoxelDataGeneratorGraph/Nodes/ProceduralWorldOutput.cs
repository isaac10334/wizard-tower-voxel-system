using UnityEngine;
using UnityEditor;
using XNodeEditor;
using XNode;
using Unity.Mathematics;
using System.Collections.Generic;

namespace VoxelDataGeneratorGraph
{
    [DisallowMultipleNodes, NodeTint(0.4f, 0.60f, 0.4f)]
    public class ProceduralWorldOutput : ProceduralWorldGraphNodeBase
    {
        public int4 testKey = new int4(0, 0, 0, 1);
    #if COLORED_CUBIC_CHUNKS || COLORED_SURFACE_NETS_CHUNKS
        [Input(typeConstraint = TypeConstraint.Strict, connectionType = ConnectionType.Multiple)] public UnityEngine.Color32 color;
    #endif
        [Input(typeConstraint = TypeConstraint.Strict)] public uint material;

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public override NodeType GetNodeType() => NodeType.UIOnly;

        public override bool IsRootNode()
        {
            return false;
        }

        public override void SetupNodeData(Dictionary<ProceduralWorldGraphNodeBase, int> lookup, NodeData nodeData)
        {
            throw new System.NotImplementedException();
        }
    }

#if UNITY_EDITOR
    [CustomNodeEditor(typeof(ProceduralWorldOutput))]
    public class ProceduralWorldOutputEditor: NodeEditor
    { 
        private double _generationMs;
        private Vector2 _previewRotation;

        public override string GetHeaderTooltip()
        {
            return "The output node of the procedural world graph.";
        }
        public override void OnHeaderGUI()
        {
            GUIStyle smallNodeTitle = new GUIStyle(EditorStyles.boldLabel);
            smallNodeTitle.fontSize = 9;
            smallNodeTitle.alignment = TextAnchor.MiddleRight;

            GUILayout.BeginHorizontal();
            GUILayout.Label(target.name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
            GUILayout.Label($"{_generationMs.ToString("F2")} ms", smallNodeTitle, GUILayout.Height(30));

            GUILayout.EndHorizontal();
        }

        const string MeshPath = "Assets/VoxelSystem/Generated/PreviewMesh.asset";
        
        public override void OnBodyGUI()
        {
            base.OnBodyGUI();
            
            ProceduralWorldOutput node = target as ProceduralWorldOutput;
            ProceduralWorldGraph graph = node.graph as ProceduralWorldGraph;

            int oldSeed = graph.DefaultSeed;
            int newSeed = EditorGUILayout.IntField("Preview Seed", oldSeed);
            if(oldSeed != newSeed)
            {
                graph.DefaultSeed = newSeed;
            }

            float oldFreq = graph.Frequency;
            float freq = EditorGUILayout.FloatField("Frequency", graph.Frequency);

            if(Mathf.Abs(oldFreq - freq) > Mathf.Epsilon)
            {
                graph.Frequency = freq;
            }
            
            int lastTextureRes = graph.TexturePreviewResolution;
            int newTextureRes = EditorGUILayout.IntField("Resolution", graph.TexturePreviewResolution);

            if(newTextureRes != lastTextureRes)
            {
                graph.TexturePreviewResolution = newTextureRes;
            }
            if(GUILayout.Button("Update Preview"))
            {   
                UpdatePreview();
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            Material mat = graph.GetPreviewMaterial();
            DrawPreview(mesh, mat);

            string errorMessage = (graph as ProceduralWorldGraph).previewErrorMessage;
            if(!string.IsNullOrWhiteSpace(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }
        private Texture2D _previewBackgroundTexture;
        private bool _isDragging = false;

        private void UpdatePreview()
        {
            ProceduralWorldGraph graph = target.graph as ProceduralWorldGraph;
            UpdatePreviewPrefab(graph);
        }

        private void UpdatePreviewPrefab(ProceduralWorldGraph graph)
        {
            string relativePath = MeshPath;
            AssetDatabase.DeleteAsset(relativePath);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            Mesh mesh = graph.GenerateAndMeshPreviewPrefab((target as ProceduralWorldOutput).testKey);
            
            _generationMs = sw.Elapsed.TotalMilliseconds;

            if(mesh == null)
            {
                if(string.IsNullOrEmpty(graph.previewErrorMessage))
                {
                    graph.previewErrorMessage = "Mesh was empty.";
                }
                return;
            }

            string fullPath = System.IO.Path.Combine(Application.dataPath + "VoxelSystem/Generated/PreviewMesh.asset");
            string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }            
            AssetDatabase.CreateAsset(mesh, relativePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region InteractablePreview
        private void DrawPreview(Mesh mesh, Material mat)
        {
            if (mesh == null) return;

            Rect previewRect = GetPreviewRect();
            GUIStyle background = GetPreviewBackground();
            PreviewRenderUtility previewRender = new PreviewRenderUtility();
            previewRender.BeginPreview(previewRect, background);

            Bounds meshBounds = mesh.bounds;
            Vector3 center = meshBounds.center;
            float cameraDistance = GetCameraDistance(meshBounds);

            SetupPreviewCamera(previewRender.camera, previewRect);
            AddLightToScene(previewRender.lights[0]);
            MakePreviewInteractable(previewRect);

            Vector3 cameraPosition = GetCameraPosition(center, cameraDistance);
            previewRender.camera.transform.position = cameraPosition;
            previewRender.camera.transform.LookAt(center);

            previewRender.DrawMesh(mesh, Vector3.zero, Quaternion.identity, mat, 0);
            previewRender.Render(true);

            Texture result = previewRender.EndPreview();
            GUI.DrawTexture(previewRect, result, ScaleMode.StretchToFill, true);
            previewRender.Cleanup();
        }

        private Rect GetPreviewRect()
        {
            return GUILayoutUtility.GetRect(200, 200);
        }
        
        private GUIStyle GetPreviewBackground()
        {
            return new GUIStyle { normal = { background = _previewBackgroundTexture } };
        }

        private float GetCameraDistance(Bounds meshBounds)
        {
            return meshBounds.extents.magnitude * 2.5f;
        }

        private void SetupPreviewCamera(Camera camera, Rect previewRect)
        {
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.fieldOfView = 60f;
            camera.aspect = previewRect.width / previewRect.height;
        }

        private void AddLightToScene(Light light)
        {
            light.intensity = 1.4f;
            light.transform.rotation = Quaternion.Euler(50f, 50f, 0);
        }

        private void MakePreviewInteractable(Rect previewRect)
        {
            EditorGUIUtility.AddCursorRect(previewRect, MouseCursor.Orbit);

            if (Event.current.type == EventType.MouseDown && previewRect.Contains(Event.current.mousePosition))
            {
                _isDragging = true;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                _isDragging = false;
                Event.current.Use();
            }

            if (_isDragging && Event.current.type == EventType.MouseDrag)
            {
                _previewRotation.y += Event.current.delta.x * 1f;
                _previewRotation.x -= Event.current.delta.y * 1f;
                _previewRotation.x = Mathf.Clamp(_previewRotation.x, -89f, 89f); // Clamp the x rotation
                Event.current.Use();
            }
        }
        private Vector3 GetCameraPosition(Vector3 center, float cameraDistance)
        {
            // Default camera position (looking down the negative z-axis)
            Vector3 defaultCameraPosition = new Vector3(0, 0, -cameraDistance);

            // Create quaternions representing the rotations around the x and y axes
            Quaternion rotationX = Quaternion.AngleAxis(-_previewRotation.x, Vector3.right); // Negate the _previewRotation.x value here
            Quaternion rotationY = Quaternion.AngleAxis(_previewRotation.y, Vector3.up);

            // Apply the rotations to the default camera position
            Vector3 rotatedCameraPosition = rotationY * rotationX * defaultCameraPosition;
            return center + rotatedCameraPosition;
        }
        #endregion
    }
#endif
}
