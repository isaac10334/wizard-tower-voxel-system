using System;
using UnityEngine;
using VoxelSystem.Meshing;

namespace VoxelSystem.Core
{
    [Serializable]
    public class VoxelSystemSettings
    {
        public Material DefaultMaterial;
        // public VoxelDataGeneratorResource VoxelDataGeneratorResource;
    }

    public class VoxelSystemSettingsStore : ScriptableObject
    {
        public VoxelSystemSettings Settings;
        public static VoxelSystemSettingsStore Default => CreateDefaultSettingsInstance();
        
        private static VoxelSystemSettingsStore CreateDefaultSettingsInstance()
        {
            var settings = CreateInstance<VoxelSystemSettingsStore>();
            settings.Settings = new VoxelSystemSettings();
            // TODO set up defalut ones
            // settings.VoxelDataGeneratorResource = null;
            // settings.VoxelMeshingResource = null;

            return settings;
        }
    }
}