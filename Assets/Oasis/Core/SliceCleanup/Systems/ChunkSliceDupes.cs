using System.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(SliceCleanupGroup))]
    public partial class ChunkSliceDupes : SystemBase
    {
        EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var children = GetBufferFromEntity<Child>(true);
            var slices = GetComponentDataFromEntity<Slice>(true);
            var chunks = GetComponentDataFromEntity<Chunk>(true);
            
            Entities
            .WithAll<DupeCheck>()
            .ForEach((Entity e, int entityInQueryIndex, in Parent parent, in Slice slice) =>
            {
                // return unless parent is chunk with children
                if (!chunks.HasComponent(parent.Value) || !children.HasComponent(parent.Value))
                    return;

                // Find and destroy duplicate slice
                // Loop through parent chunk child slices
                var siblings = children[parent.Value];
                for (int i = 0; i < siblings.Length; i++)
                {
                    var sibling = siblings[i].Value;
                    
                    // if sibling not slice...continue
                    if (!slices.HasComponent(sibling))
                        continue;
                    
                    
                    // if sibling is same slice and older ... destroy it
                    var siblingSlice = slices[sibling];
                    if (sibling != e && siblingSlice.Axis == slice.Axis && siblingSlice.Depth == slice.Depth)
                    {
                        Debug.Log($"Destroying dupe {sibling}");
                        if (children.HasComponent(sibling))
                        {
                            var faces = children[sibling];
                            for (int j = faces.Length - 1; j >= 0; j--)
                                ecb.DestroyEntity(entityInQueryIndex, faces[j].Value);
                        }
                                
                        // Destroy sibling slice itself
                        ecb.DestroyEntity(entityInQueryIndex, sibling);
                    }
                }

                ecb.RemoveComponent<DupeCheck>(entityInQueryIndex, e);
            })
            .WithReadOnly(chunks)
            .WithReadOnly(slices)
            .WithReadOnly(children)
            .ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(this.Dependency);
        }




        protected override void OnCreate()
        {
            _ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

    }
    
}
