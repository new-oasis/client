using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Oasis.Grpc;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using BovineLabs.Event.Containers;
using BovineLabs.Event.Jobs;
using BovineLabs.Event.Systems;


namespace Oasis.Core
{
    public partial class PlayerChunkChangeEventHandler : ConsumeSingleEventSystemBase<PlayerChunkChangeEvent>
    {
        private int range = 27;

        protected override void OnEvent(PlayerChunkChangeEvent playerChunkChangeEvent)
        {
            var realm = GetSingleton<PlayerRealm>();
            Debug.Log($"playerChunkChangeEventHandler {realm.Value} {playerChunkChangeEvent.Value} and playerRealm");
            
            
            var chunks = World.GetExistingSystem<ChunkSystem>()._entities;
            var settings = GetSingleton<Settings>();

            // Compute chunks in range
            var chunksInRange = new NativeList<int3>(Allocator.Temp);
            ListChunksInRange(playerChunkChangeEvent.Value, chunksInRange, settings.distance);


            // Create chunks
            var prefab = GetSingleton<ChunkPrefab>().Value;
            for (var i = 0; i < chunksInRange.Length; i++)
            {
                var id = chunksInRange[i];
                if (!chunks.ContainsKey(id))
                {
                    var newChunk = EntityManager.Instantiate(prefab);
                    EntityManager.AddComponentData(newChunk, new Chunk() {id = id});
                    EntityManager.AddComponentData(newChunk, new Translation {Value = new float3(id * 16)});
                    EntityManager.AddComponentData(newChunk, new CreateSlices { });
                    EntityManager.AddComponentData(newChunk, new LoadTag { });

                    EntityManager.AddSharedComponentData(newChunk, new Realm{Value = realm.Value});

                    if (i < range)
                    {
                        EntityManager.AddComponentData(newChunk, new VisibleTag { lit = true});
                        EntityManager.RemoveComponent<PhysicsCollider>(newChunk);
                    }

                    chunks.TryAdd(id, newChunk);
                }
            }

            // TODO Destroy oob chunks
        }

        private static void ListChunksInRange(int3 player, NativeList<int3> chunksInRange, int distance)
        {
            var radius = distance / 2;
            for (var x = player.x - radius; x <= player.x + radius; x++)
            for (var y = player.y - radius; y <= player.y + radius; y++)
            for (var z = player.z - radius; z <= player.z + radius; z++)
            {
                if (y < 0) continue;
                chunksInRange.Add(new int3(x, y, z));
            }

            var comparer = new DistanceCompare(player);
            chunksInRange.AsArray().Sort(comparer);
        }

        private struct DistanceCompare : IComparer<int3>
        {
            private int3 origin;

            public DistanceCompare(int3 arg)
            {
                origin = arg;
            }

            public int Compare(int3 a, int3 b)
            {
                if ((a - origin).Magnitude() > (b - origin).Magnitude())
                    return 1;
                else
                    return -1;
            }
        }
    }
}