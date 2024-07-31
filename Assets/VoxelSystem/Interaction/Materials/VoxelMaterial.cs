using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{

    [CreateAssetMenu(menuName = "VoxelSystem/VoxelMaterial")]
    public class VoxelMaterial : ScriptableObject
    {
        public Texture2D XNegative;
        public Texture2D YNegative;
        public Texture2D ZNegative;
        public Texture2D XPositive;
        public Texture2D YPositive;
        public Texture2D ZPositive;
    }
}
