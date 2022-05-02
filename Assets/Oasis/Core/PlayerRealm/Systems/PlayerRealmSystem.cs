using System.Threading.Tasks;
using BovineLabs.Event.Systems;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using DomainName = Oasis.Core.DomainName;

public partial class PlayerRealmSystem : SystemBase
{
    protected override void OnUpdate()
    {
    }

    public void SetRealm(DomainName realm)
    {
        var playerEntity = GetSingletonEntity<FirstPersonPlayer>();
        if (!HasComponent<PlayerRealm>(playerEntity))
            World.EntityManager.AddComponent(playerEntity, typeof(PlayerRealm));
        SetComponent(playerEntity, new PlayerRealm {Value = realm});
        
        Client.Instance.feedRequest.WriteAsync(new FeedRequest(){JoinRealm = new Oasis.Grpc.DomainName(realm.ToGrpc())});

        var eventSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EventSystem>();
        var playerRealmChangeEventWriter = eventSystem.CreateEventWriter<PlayerRealmChangeEvent>();
        playerRealmChangeEventWriter.Write(new PlayerRealmChangeEvent {Value = realm});
        eventSystem.AddJobHandleForProducer<PlayerRealmChangeEvent>(this.Dependency);
    }
}