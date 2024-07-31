#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using CsharpVoxReader;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace VoxelSystem
{
    [UnityEditor.AssetImporters.ScriptedImporter(1, "vox")]
    public class MagicaVoxelImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string assetPath = ctx.assetPath;

            if (!(Path.GetExtension(assetPath).ToLower() == ".vox")) return;

            VoxLoader myVoxLoader = new VoxLoader();
            VoxReader r = new VoxReader(assetPath, myVoxLoader);
            r.Read();

            MagicaVoxelFile magicaVoxelFile = ScriptableObject.CreateInstance<MagicaVoxelFile>();
            magicaVoxelFile.Initialize(assetPath);
            
            List<VoxObject> voxObjects = myVoxLoader.GetVoxObjects();
            magicaVoxelFile.CreateFromVoxObjects(voxObjects);
            
            ctx.AddObjectToAsset("ImportedMagicaVoxelFile", magicaVoxelFile);
            ctx.SetMainObject(magicaVoxelFile);
        }
    }
}
#endif