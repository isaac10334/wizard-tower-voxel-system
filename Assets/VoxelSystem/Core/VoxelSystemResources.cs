using System;
using Unity.Collections;
using UnityEngine;

namespace VoxelSystem.Core
{
    public partial class VoxelSystemResources : MonoBehaviour
    {
        public VoxelSystemSettingsStore settingsStore;

        private void CreateSharedResources()
        {
            if (settingsStore == null) throw new NullReferenceException("Set up VoxelSystem settings.");
        }
    }
}