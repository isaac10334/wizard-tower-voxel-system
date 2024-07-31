using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace VoxelSystem
{
    [CustomEditor(typeof(MaterialsDatabase))]
    public class MaterialsDatabaseEditor : UnityEditor.Editor
    {
        SerializedProperty materials;

        public void OnEnable()
        {
            materials = serializedObject.FindProperty("materials");
        }

        public void OnDisable()
        {

        }
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(materials, new GUIContent("Materials"));
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                Debug.Log("Text field has changed.");
            }
        }
    }
}
