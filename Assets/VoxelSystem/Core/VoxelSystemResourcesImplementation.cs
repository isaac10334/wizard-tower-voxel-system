using System;
using UnityEngine;

namespace VoxelSystem.Core
{
    public partial class VoxelSystemResources : MonoBehaviour
    {
        private static VoxelSystemResources _instance;

        public static VoxelSystemResources Instance => _instance;


        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // Ensure only one instance exists.
            }
            else
            {
                _instance = this;
            }

            CreateSharedResources();
        }
    }
}