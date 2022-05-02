using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Rendering.HybridV2;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;


namespace Oasis.Core
{
    public static class EntityHelpers
    {
        public static void SetLayers(Entity e, string layer)
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Entity
            if (em.HasComponent<RenderMesh>(e))
            {
                RenderMesh rm = em.GetSharedComponentData<RenderMesh>(e);
                rm.layer = LayerMask.NameToLayer(layer);
                em.SetSharedComponentData<RenderMesh>(e, rm);
            }

            // Children
            if (em.HasComponent<Child>(e))
            {
                NativeArray<Entity> children =
                    em.GetBuffer<Child>(e).Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
                foreach (Entity child in children)
                {
                    if (em.HasComponent<RenderMesh>(child))
                    {
                        RenderMesh rm = em.GetSharedComponentData<RenderMesh>(child);
                        rm.layer = LayerMask.NameToLayer(layer);
                        em.SetSharedComponentData<RenderMesh>(child, rm);
                    }

                    SetLayers(child, layer); // Recurse through child
                }
            }
        }

        public static async Task WaitForDependencies(Entity e)
        {
            while (!World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<LoadedDependenciesTag>(e))
                await Task.Yield();
        }
        
        public static void DestroyWithChildren(Entity e)
        {
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (em.HasComponent<Child>(e))
            {
                DynamicBuffer<Child> childrenBuffer = em.GetBuffer<Child>(e);
                NativeArray<Entity> children = childrenBuffer.Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
                foreach (Entity child in children)
                    DestroyWithChildren(child); // Recurse through child
            }

            em.DestroyEntity(e);
        }

        // Shift side renderMeshs
        public static void Recenter(Entity e, int3 dims)
        {
            // Update the below


            //     float3 adjustment = new float3(dims) /2.0f;
            //     EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            //     if (em.HasComponent<Child>(e))
            //     {
            //         DynamicBuffer<Child> childrenBuffer = em.GetBuffer<Child>(e);
            //         NativeArray<Entity> children = childrenBuffer.Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
            //         foreach (Entity child in children)
            //         {
            //             if (em.HasComponent<RenderMesh>(child))
            //             {
            //                 RenderMesh rm = em.GetSharedComponentData<RenderMesh>(child);
            //                 Mesh mesh = rm.mesh;

            //                 // Adjust verts;  
            //                 // Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
            //                 Vector3 offset = new Vector3(adjustment.x, adjustment.y, adjustment.z);
            //                 Vector3[] verts = mesh.vertices;
            //                 for (var i = 0; i < verts.Length; i++)
            //                 {
            //                     verts[i] = verts[i] - offset;
            //                 }
            //                 rm.mesh.vertices = verts;
            //                 em.SetSharedComponentData<RenderMesh>(child, rm);
            //             }
            //             Recenter(child, dims); // Recurse through child
            //         }
            //     }
        }

    }
}