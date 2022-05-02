using System.Threading.Tasks;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using DomainName = Oasis.Core.DomainName;
using Entity = Unity.Entities.Entity;

namespace Oasis.Game
{
    using Debug = UnityEngine.Debug;
    
    public class GameController : MonoBehaviour
    {
        public string realmDomain;
        public string realmName;
        public string placeName = "start";
        
        private EntityManager _em;

        private void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private async void Start()
        {
            await Task.Yield(); // Wait for conversion systems
            
            // Load Start place
            var gDomainName = new Grpc.DomainName() { Domain = realmDomain, Name = realmName };
            var placeRequest = new Grpc.PlaceRequest(){Realm = gDomainName, Name = placeName};
            var gPlaces = await Client.Instance.client.SearchPlacesAsync(placeRequest, Client.Instance.Metadata);
            var gPlace = gPlaces.Value[0];
            
            // Set Player realm
            var playerRealmSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PlayerRealmSystem>();
            playerRealmSystem.SetRealm(new DomainName(realmDomain, realmName));
            
            // Move to start
            var playerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PlayerSystem>();
            await playerSystem.Move(new int3().FromInt3(gPlace.Xyz));
            
            // Remove Player LoadingTag...so PlayerChunk starts working
            var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(FirstPersonPlayer));
            var playerEntity = query.GetSingletonEntity();
            World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<LoadingTag>(playerEntity);
        }
     
    }
}
