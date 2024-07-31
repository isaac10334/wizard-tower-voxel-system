#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using VoxelSystem.Core;

namespace VoxelSystem.Core.Editor
{
    [CustomEditor(typeof(VoxelSystemResources))]
    public class VoxelSystemResourcesEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor scriptableObjectEditor;

        public override void OnInspectorGUI()
        {
            VoxelSystemResources myComponent = (VoxelSystemResources)target;
            // Draw the default inspector for the MonoBehaviour
            DrawDefaultInspector();

            // Check if there's a ScriptableObject assigned
            if (myComponent.settingsStore != null)
            {
                DrawScriptableObject(myComponent.settingsStore);
            }
            else
            {
                if (GUILayout.Button("Create VoxelSystem Settings Object"))
                {
                    // Create a default VoxelSystemSettings object
                    VoxelSystemSettingsStore settingsStore = VoxelSystemSettingsStore.Default;

                    // Define the path where the asset should be saved
                    string assetPath = "Assets/VoxelSystemSettings.asset";

                    // Check if an asset already exists at that path to avoid overwriting it
                    VoxelSystemSettingsStore existingSettingsStore =
                        AssetDatabase.LoadAssetAtPath<VoxelSystemSettingsStore>(assetPath);
                    if (existingSettingsStore == null)
                    {
                        // Create the asset in the project
                        AssetDatabase.CreateAsset(settingsStore, assetPath);

                        // Save the assets and refresh the AssetDatabase
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        // Load the newly created asset
                        myComponent.settingsStore = AssetDatabase.LoadAssetAtPath<VoxelSystemSettingsStore>(assetPath);
                    }
                    else
                    {
                        myComponent.settingsStore = existingSettingsStore;
                    }

                    EditorUtility.SetDirty(myComponent);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawScriptableObject(ScriptableObject scriptableObject)
        {
            // Create an editor for the ScriptableObject if it doesn't exist or if the reference has changed
            if (scriptableObjectEditor == null || scriptableObjectEditor.target != scriptableObject)
            {
                DestroyImmediate(scriptableObjectEditor); // Destroy the old editor if it exists
                scriptableObjectEditor = CreateEditor(scriptableObject);
            }

            // Use the ScriptableObject editor to draw its inspector UI
            if (scriptableObjectEditor != null)
            {
                scriptableObjectEditor.OnInspectorGUI();
            }
        }

        private void OnDisable()
        {
            // Clean up the editor when it's not needed to avoid memory leaks
            if (scriptableObjectEditor != null)
            {
                DestroyImmediate(scriptableObjectEditor);
            }
        }
    }
}
#endif