using Unity.Entities;
using UnityEngine;
using Oasis.Core;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class ExampleChunk : MonoBehaviour
{
    public string realmDomain;
    public string realmName;
    public int3 id;

    void Start()
    {
        var blockStates = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BlockStateSystem>();
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var chunks = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ChunkSystem>();
        
        
        // Create chunk
        var chunkQuery = em.CreateEntityQuery(ComponentType.ReadOnly<ChunkPrefab>());
        var chunkPrefab = chunkQuery.GetSingleton<ChunkPrefab>().Value;
        
        var e = em.Instantiate(chunkPrefab);
        em.AddComponentData(e, new Translation { Value = new float3(id*16) });
        em.AddComponentData(e, new CreateSlices { });
        em.AddComponentData(e, new LoadTag { });
        em.AddComponentData(e, new Chunk() {id = id});
        
        var domainName = new DomainName{domain = realmDomain, name = realmName};
        em.AddSharedComponentData(e, new Realm() {Value = domainName});

        var chunkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ChunkSystem>();
        chunkSystem._entities[id] = e;
        em.AddComponent<VisibleTag>(e);
    }
}