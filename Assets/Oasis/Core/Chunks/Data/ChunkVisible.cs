using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Oasis.Core;
using Random = UnityEngine.Random;
using RaycastHit = Unity.Physics.RaycastHit;

 
namespace Oasis.Core
{
     [UpdateInGroup(typeof(ChunkGroup))]
     // [UpdateAfter(typeof(ChunkCreate))]
     public partial class ChunkVisible : SystemBase
     {
         private BuildPhysicsWorld buildPhysicsWorld;
         private CollisionWorld collisionWorld;
         private CollisionFilter collisionFilter;
         private EndInitializationEntityCommandBufferSystem ecbSystem;
         private float rayDistance = 100f;
         private EntityQuery query;
         private ChunkSystem _chunkSystem;

         protected override void OnCreate()
         {
             _chunkSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis.Core.ChunkSystem>();
             ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
             buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
             collisionFilter = new CollisionFilter
             {
                 BelongsTo = 1u << 2,
                 CollidesWith = 1u << 2 | 1u << 4,
                 GroupIndex = 0,
             };
         
             RequireSingletonForUpdate<FirstPersonCharacterComponent>();
             base.OnCreate();
         }

         protected override void OnUpdate()
         {
             var ecb = ecbSystem.CreateCommandBuffer();
             
             // Step one.  Prepare job
             int i = 0;
             int divisor = 5;
             float fraction = 1.0f * 2 / divisor;
             int rayCount = divisor * divisor;
             NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(rayCount, Allocator.TempJob);
             NativeArray<RaycastInput> commands = new NativeArray<RaycastInput>(rayCount, Allocator.TempJob);
             NativeArray<bool> hit = new NativeArray<bool>(rayCount, Allocator.TempJob);
             
             for (float x = 0f; x < 1f; x += fraction)
             {
                 for (float y = 0f; y < 1f; y += fraction)
                 {
                     var randx = Random.Range(0f - fraction, fraction);
                     var randy = Random.Range(0f - fraction, fraction);
                     UnityEngine.Ray ray = Camera.main.ViewportPointToRay(new Vector3(x + randx, y + randy,0f));
                     Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.blue);
                 
                     RaycastInput raycastInput = new RaycastInput()
                     {
                         Start = ray.origin, 
                         End = ray.origin + ray.direction * rayDistance,
                         Filter = collisionFilter
                     };
                     commands[i] = raycastInput;
                     i = i + 1;
                 }
             }
             
             
             // Step two.  Run job
             var handle = ScheduleBatchRayCast(buildPhysicsWorld.PhysicsWorld.CollisionWorld, commands, results);
             handle.Complete();
             commands.Dispose();
             
             // Step three. Process results
             foreach (RaycastHit result in results)
             {
                 Entity hitEntity = buildPhysicsWorld.PhysicsWorld.Bodies[result.RigidBodyIndex].Entity;
                 if (!HasComponent<VisibleTag>(hitEntity) && HasComponent<Chunk>(hitEntity))
                     EntityManager.AddComponentData(hitEntity, new VisibleTag{lit = true});  // Parallelize
             }
             results.Dispose();
             hit.Dispose();
         }


         struct RaycastJob : IJobParallelFor
         {
             [ReadOnly] public CollisionWorld CollisionWorld;
             [ReadOnly] public NativeArray<RaycastInput> inputs;
             public NativeArray<RaycastHit> results;
     
             public void Execute(int index)
             {
                 RaycastHit hit;
                 CollisionWorld.CastRay(inputs[index], out hit);
                 results[index] = hit;
             } 
         }
     
     
     
         JobHandle ScheduleBatchRayCast(CollisionWorld CollisionWorld, NativeArray<RaycastInput> inputs, NativeArray<RaycastHit> results)
         {
             JobHandle rcj = new RaycastJob
             {
                 inputs = inputs,
                 results = results,
                 CollisionWorld = CollisionWorld
             }.Schedule(inputs.Length, 4);
             
             return rcj;
         }
         
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

         
     }
}