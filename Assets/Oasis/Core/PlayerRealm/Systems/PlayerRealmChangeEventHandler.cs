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
    public partial class PlayerRealmChangeEventHandler : ConsumeSingleEventSystemBase<PlayerRealmChangeEvent>
    {

        protected override void OnEvent(PlayerRealmChangeEvent playerRealmChangeEvent)
        {
            Debug.Log($"Got realm change event. Destroying chunks not in {playerRealmChangeEvent.Value} realm");
            var chunkSystem = World.GetExistingSystem<ChunkSystem>();
            
            // Destroy realm chunks and children
            Entities
                .ForEach((Entity e, ref Chunk chunk, in Realm realm) =>
                {
                    if (!realm.Value.Equals(playerRealmChangeEvent.Value))
                    {
                        // Debug.Log("Destroying " + chunk.id);
                        EntityHelpers.DestroyWithChildren(e);
                        chunkSystem._entities.Remove(chunk.id);
                    }
                }).WithStructuralChanges().Run();
        }

    }
}