using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Oasis/Variable/RuntimeDict")]
public abstract class RuntimeDict<I,T> : ScriptableObject
{
    public Dictionary<I,T> Items; // = new Dictionary<I,T>();

    void Awake() { }


    void OnEnable()
    {
        Items = new Dictionary<I, T>();
    }

    void OnDisable()
    {
        Items = new Dictionary<I, T>(); // Not called on stop..
    }

    public void Add(I i, T t)
    {
        if (!Items.ContainsKey(i))
            Items.Add(i, t);
    }

    public void Remove(I i)
    {
        if (Items.ContainsKey(i))
            Items.Remove(i);
    }
}
