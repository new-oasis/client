using System.Collections.Generic;
using Oasis.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class DebugChunkLoading : SystemBase
{
    private Dictionary<Entity, int> loading;

    protected override void OnCreate()
    {
        loading = new Dictionary<Entity, int>();
    }

    protected override void OnUpdate()
    {
        
        Entities
            .WithNone<Missing>()
            .ForEach((ref Entity e, ref LoadingTag loadingTags, ref Chunk chunk) =>
        {
            if (!loading.ContainsKey(e))
                loading[e] = 0;
            loading[e] += 1;
        }).WithoutBurst().Run();

        List<Entity> toRemove = new List<Entity>();
        foreach (var l in loading)
        {
            if (!HasComponent<LoadingTag>(l.Key))
                toRemove.Add(l.Key);

            var chunk = GetComponent<Chunk>(l.Key);
            if (l.Value > 50)
                Debug.Log($"{chunk.id.ToString()} chunk stuck loading");
        }
        foreach (var entity in toRemove)
            loading.Remove(entity);
    }
}
