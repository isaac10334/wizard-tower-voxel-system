using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelSystem
{
    [CreateAssetMenu(menuName = "VoxelSystem/MaterialsDatabase")]
    public class MaterialsDatabase : ScriptableObject
    {
        public Texture2D textureAtlas;
        public VoxelMaterial[] materials;

        public Texture2D[] GetTextures()
        {
            List<Texture2D> textures = new List<Texture2D>();
            
            foreach(VoxelMaterial material in materials)
            {
                textures.Add(material.XNegative);
                textures.Add(material.YNegative);
                textures.Add(material.ZNegative);
                textures.Add(material.XPositive);
                textures.Add(material.YPositive);
                textures.Add(material.ZPositive);
            }

            return textures.ToArray();
        }
        
        public Texture2D GetTextureAtlas()
        {
            Texture2D[] textures = GetTextures();
            Texture2D textureAtlas = new Texture2D(8192, 8192, TextureFormat.RGB24, false);
            textureAtlas.PackTextures(textures, 0, 8192);
            return textureAtlas;
        }

        public uint GetIDFromMaterial(VoxelMaterial material)
        {
            for(int i = 0; i < materials.Length; i++)
            {
                if(materials[i] == material) return (uint)i;
            }

            throw new System.ArgumentException("Material is not in database.");
        }
    }
}
