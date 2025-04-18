using Unity.Entities;
using UnityEngine;
using Oasis.Core;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class ExampleChunk : MonoBehaviour
{
    public string realmVersion;
    public string realmDomain;
    public string realmName;
    public int3 id;
    public int3 range;
    public bool lit;

    void Start()
    {
        var blockStates = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BlockStateSystem>();
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var chunks = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ChunkSystem>();

        var chunkQuery = em.CreateEntityQuery(ComponentType.ReadOnly<ChunkPrefab>());
        var chunkPrefab = chunkQuery.GetSingleton<ChunkPrefab>().Value;

        Debug.Log($"{id}");
        Debug.Log($"{range}");
        Debug.Log($"{id.x - range.x}");
        Debug.Log($"{id.x + range.x}");

        for (int x = id.x - range.x; x <= id.x + range.x; x++)
        {
            for (int y = id.y - range.y; y <= id.y + range.y; y++)
            {
                for (int z = id.z - range.z; z <= id.z + range.z; z++)
                {
                    Debug.Log($"{x},{y},{z}");

                    var id = new int3 {x = x, y = y, z = z};
                    var e = em.Instantiate(chunkPrefab);
                    em.AddComponentData(e, new Translation { Value = id.FlipZ() * 16 });
                    em.AddComponentData(e, new CreateSlices { });
                    em.AddComponentData(e, new LoadTag { });
                    em.AddComponentData(e, new Chunk() {id = id});

                    var domainName = new DomainName {version = realmVersion, domain = realmDomain, name = realmName};
                    em.AddSharedComponentData(e, new Realm() {Value = domainName});

                    var chunkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ChunkSystem>();
                    chunkSystem._entities[id] = e;
                    em.AddComponentData(e, new VisibleTag{lit = lit});
                }
            }
        }
    }
}