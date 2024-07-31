using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[Serializable]
public class SerializableNativeParallelHashMap<TKey, TValue> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
{
    [SerializeField]
    private TKey[] keys;

    [SerializeField]
    private TValue[] values;

    public SerializableNativeParallelHashMap(NativeParallelHashMap<TKey, TValue> nativeParallelHashMap)
    {
        Serialize(nativeParallelHashMap);
    }

    private void Serialize(NativeParallelHashMap<TKey, TValue> nativeParallelHashMap)
    {
        int length = nativeParallelHashMap.Count();

        keys = new TKey[length];
        values = new TValue[length];

        int index = 0;
        var keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keyArray.Length; i++)
        {
            TKey key = keyArray[i];
            if (nativeParallelHashMap.TryGetValue(key, out TValue value))
            {
                keys[index] = key;
                values[index] = value;
                index++;
            }
        }
        keyArray.Dispose();
    }

    public NativeParallelHashMap<TKey, TValue> Deserialize(Allocator allocator)
    {
        int length = keys.Length;
        NativeParallelHashMap<TKey, TValue> nativeParallelHashMap = new NativeParallelHashMap<TKey, TValue>(length, allocator);

        for (int i = 0; i < length; i++)
        {
            nativeParallelHashMap.TryAdd(keys[i], values[i]);
        }

        return nativeParallelHashMap;
    }
}