using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using CsharpVoxReader;
using VoxelSystem.Colors;
using Chunks = CsharpVoxReader.Chunks;

public class VoxObject
{
    public int3 position;
    public int sizeX;
    public int sizeY;
    public int sizeZ;
    public byte[,,] bytes;
    public uint[,,] colors;

    public VoxObject(int3 position, int sizeX, int sizeY, int sizeZ, byte[,,] bytes)
    {
        this.position = position;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.bytes = bytes;
        this.colors = new uint[sizeX, sizeY, sizeZ];
    }
}

class VoxLoader : IVoxLoader
{
    private List<VoxObject> _voxObjects = new List<VoxObject>();

    public List<VoxObject> GetVoxObjects()
    {
        return _voxObjects;
    }

    public void LoadModel(Int32 sizeX, Int32 sizeY, Int32 sizeZ, byte[,,] data)
    {
        Debug.Log("Loaded model.");

        VoxObject v = new VoxObject(int3.zero, sizeX, sizeY, sizeZ, data);

        _voxObjects.Add(v);
    }

    public void LoadPalette(UInt32[] palette)
    {
        foreach (VoxObject v in _voxObjects)
        {
            for (int x = 0; x < v.sizeX; x++)
            {
                for (int y = 0; y < v.sizeY; y++)
                {
                    for (int z = 0; z < v.sizeZ; z++)
                    {
                        uint paletteIndex = v.bytes[x, y, z];

                        // replace v.data with actual color instead of pallette index
                        // use VoxelColor32 uint logic
                        palette[paletteIndex].ToARGB(out byte a, out byte r, out byte g, out byte b);
                        var color = new Color32(r, g, b, a);
                        ushort color16 = ColorUtilities.ConvertColor32To16Bit(color);
                        v.colors[x, y, z] = color16;
                    }
                }
            }
        }
    }

    public void SetModelCount(Int32 count)
    {
    }

    public void SetMaterialOld(Int32 paletteId, CsharpVoxReader.Chunks.MaterialOld.MaterialTypes type, float weight,
        CsharpVoxReader.Chunks.MaterialOld.PropertyBits property, float normalized)
    {
    }

    // VOX Extensions
    public void NewTransformNode(Int32 id, Int32 childNodeId, Int32 layerId, string name,
        Dictionary<string, byte[]>[] framesAttributes)
    {
    }

    public void NewGroupNode(Int32 id, Dictionary<string, byte[]> attributes, Int32[] childrenIds)
    {
    }

    public void NewShapeNode(Int32 id, Dictionary<string, byte[]> attributes, Int32[] modelIds,
        Dictionary<string, byte[]>[] modelsAttributes)
    {
    }

    public void NewMaterial(Int32 id, Dictionary<string, byte[]> attributes)
    {
    }

    public void NewLayer(Int32 id, string name, Dictionary<string, byte[]> attributes)
    {
    }
}