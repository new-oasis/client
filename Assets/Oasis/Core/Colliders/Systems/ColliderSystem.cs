using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

// Needed to display debug info

namespace Oasis.Game
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class ColliderSystem : SystemBase
    {
        private readonly int AddColliderDistance = 24;
        // private int _removeColliderDistance = 30;


        protected override void OnCreate()
        {
            RequireSingletonForUpdate<FirstPersonCharacterComponent>();
        }
        
        protected override void OnUpdate()
        {
            
            // Get player location
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var firstPersonPlayer = GetSingleton<FirstPersonPlayer>();
            var playerTranslation = em.GetComponentData<Translation>(firstPersonPlayer.ControlledCharacter);

            // Setup job to add collider
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithNone<PhysicsCollider>()
                .WithAny<FaceTag,ModelElementFace>()
                .ForEach((Entity e, in RenderMesh rm, in NonUniformScale nus, in WorldRenderBounds wrb) =>
                {
                    var aabb = wrb.Value;
                    
                    // TODO optimize range of colliders
                    if (!(math.distance(aabb.Center, playerTranslation.Value) < AddColliderDistance)) return;
                    var blobCollider = new NativeArray<BlobAssetReference<Collider>>(1, Allocator.TempJob);
                    var nVerts = new NativeArray<Vector3>(rm.mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
                    var nTris = new NativeArray<int>(rm.mesh.triangles, Allocator.TempJob);
                    
                    
                    var v = new NativeArray<float3>(rm.mesh.vertices.Length, Allocator.TempJob);
                    for (var i = 0; i < rm.mesh.vertices.Length; i += 1)
                        v[i] = nus.Value * rm.mesh.vertices[i];
                        
                    var colliderJob = new CreateMeshColliderJob{MeshVerts = v, MeshTris = nTris, BlobCollider = blobCollider};
                    colliderJob.Run();
                    v.Dispose();
                    
                    EntityManager.AddComponentData(e, new PhysicsCollider {Value = blobCollider[0]});
                    EntityManager.AddSharedComponentData(e, new PhysicsWorldIndex{Value = 0});
                    nVerts.Dispose();
                    nTris.Dispose();
                    blobCollider.Dispose();
                }).Run();
            
            Entities
                .WithStructuralChanges()
                .WithoutBurst()
                .WithAll<PhysicsCollider>()
                .WithAll<FaceTag>()
                .ForEach((Entity e, in NonUniformScale nus, in WorldRenderBounds wrb) =>
                {
                    // AABB aabb = wrb.Value;
                    // if (math.distance(aabb.Center, playerTranslation.Value) > RemoveColliderDistance)
                    //     EntityManager.DestroyEntity(e);

                }).Run();
                    
            
            
        }
    }
}