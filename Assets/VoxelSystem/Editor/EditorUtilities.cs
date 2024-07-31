#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class EditorUtilities
{
    public static T[] GetAllInstances<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets("t:"+ typeof(T).Name);
        T[] a = new T[guids.Length];

        for(int i =0;i<guids.Length;i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return a;
    }

    public static string FindFileInAssets(string filename)
    {
        string directory = Application.dataPath;
        string[] files = Directory.GetFiles(directory, filename, SearchOption.AllDirectories);

        // Check if the file was found
        if (files.Length == 0)
        {
            // Log an error and return null
            Debug.LogError($"File not found: {filename}");
            return null;
        }

        if(files.Length > 1)
        {
            Debug.LogError($"Multiple files found: {filename}");
            return null;
        }

        // Get the first matching file
        string file = files[0];
        return file;
    }
}
#endif