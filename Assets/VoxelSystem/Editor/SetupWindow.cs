#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelSystem
{
    public class SetupWindow : EditorWindow
    {
        private static SetupWindow _editorWindow;

        [MenuItem("VoxelSystem/Setup")]
        public static void ShowSetupWindow()
        {
            _editorWindow = GetWindow<SetupWindow>(true);
            _editorWindow.minSize = new Vector2(500, 500);
            _editorWindow.titleContent = new GUIContent("Voxel System Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Thanks for purchasing Voxel System! No setup is currently required, you're good to go.");
            if(GUILayout.Button("Open Documentaiton")) OpenDocumentation();
        }

        [MenuItem("VoxelSystem/Open Documentation")]
        public static void OpenDocumentation()
        {
            string filePath = "VoxelSystemDocumentation.pdf";
            string fullPath = EditorUtilities.FindFileInAssets(filePath);

            if (!string.IsNullOrEmpty(fullPath))
            {
                Application.OpenURL(fullPath);
            }
            else
            {
                Debug.LogError($"File not found: {filePath}");
            }
        }
    }
}
#endif