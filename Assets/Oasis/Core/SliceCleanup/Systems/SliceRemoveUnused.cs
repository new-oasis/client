using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(SliceCleanupGroup))]
    public partial class SliceRemoveUnused : SystemBase
    {
        EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var childBuffers = GetBufferFromEntity<Child>(false);

            // Destroy sliceSide if zero child faces
            Entities
            .WithNone<ComputeFaces>()
            .WithAll<RemoveUnused>()
            .ForEach((Entity e, int entityInQueryIndex, in Slice Slice) =>
            {
                if (!childBuffers.HasComponent(e))
                    ecb.DestroyEntity(entityInQueryIndex, e);
                else
                {
                    var children = childBuffers[e];
                    if (children.Length == 0)
                        ecb.DestroyEntity(entityInQueryIndex, e);
                }
                ecb.RemoveComponent<RemoveUnused>(entityInQueryIndex, e);

            })
            .WithReadOnly(childBuffers)
            .ScheduleParallel();


            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }




    }
}
