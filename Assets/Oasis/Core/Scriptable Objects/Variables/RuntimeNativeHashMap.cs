using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

[CreateAssetMenu(menuName = "Oasis/Variable/RuntimeNativeHashMap")]
public abstract class RuntimeNativeHashMap<I,T> : ScriptableObject where I:struct,IEquatable<I> where T:struct
{
    public NativeParallelHashMap<I,T> Items; // = new Dictionary<I,T>();

    void OnEnable()
    {
        Items = new NativeParallelHashMap<I, T>(512, Allocator.Persistent); // TODO 512 is a guess
    }

    void OnDisable()
    {
        if (Items.IsCreated)
            Items.Dispose();
    }

}
