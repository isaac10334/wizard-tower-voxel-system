using Unity.Collections;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using VoxelSystem;
using Unity.Burst;

public struct MagicaVoxelObjectCollection : IDisposable
{
    public bool IsCreated
    {
        get { return voxelDataMap.IsCreated; }
    }
    private NativeParallelMultiHashMap<int, Chunk16> voxelDataMap;

    [BurstDiscard]
    public static MagicaVoxelObjectCollection Create(int initialCapacity, Allocator allocator)
    {
        var mvoc = new MagicaVoxelObjectCollection();
        mvoc.voxelDataMap = new NativeParallelMultiHashMap<int, Chunk16>(initialCapacity, allocator);
        return mvoc;
    }
    
    [BurstDiscard]
    public int GetOrAdd(Dictionary<MagicaVoxelFile, int> _magicaVoxelFileIDMap, MagicaVoxelFile magicaVoxelFile)
    {
        if(_magicaVoxelFileIDMap.TryGetValue(magicaVoxelFile, out int id)) return id;
        int newId = _magicaVoxelFileIDMap.Count + 1;
        _magicaVoxelFileIDMap.Add(magicaVoxelFile, newId);
        AddMagicaVoxelFile(magicaVoxelFile, newId);
        return newId;
    }

    [BurstDiscard]
    private void AddMagicaVoxelFile(MagicaVoxelFile magicaVoxelFile, int newId)
    {
        if(magicaVoxelFile == null) return;

        if(magicaVoxelFile.chunks == null || magicaVoxelFile.chunks.Count == 0) 
        {
            UnityEngine.Debug.Log($"Not adding empty magicaVoxelFile {System.IO.Path.GetFileName(magicaVoxelFile.assetPath)}.");
            return;
        }

        foreach(SerializedChunk16 chunk in magicaVoxelFile.chunks)
        {
            voxelDataMap.Add(newId, chunk.ToChunk16(Allocator.Persistent));
        }
    }
    
    public void AddVoxelData(int id, Chunk16 voxelData)
    {
        voxelDataMap.Add(id, voxelData);
    }
    
    public NativeArray<Chunk16> GetVoxelDataArray(int id, Allocator allocator)
    {
        if (!voxelDataMap.TryGetFirstValue(id, out Chunk16 value, out NativeParallelMultiHashMapIterator<int> iterator))
        {
            throw new ArgumentOutOfRangeException($"Argument {nameof(id)} out of range.");
        }
        
        var voxelDataList = new NativeList<Chunk16>(allocator) { value };

        while (voxelDataMap.TryGetNextValue(out value, ref iterator))
        {
            voxelDataList.Add(value);
        }

        return voxelDataList.AsArray();
    }
    
    public int RemoveVoxelData(int id)
    {
        return voxelDataMap.Remove(id);
    }

    public void Dispose()
    {
        voxelDataMap.Dispose();
    }
}
