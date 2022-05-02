using System.Threading.Tasks;
using Oasis.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

// move FaceChunk to FaceChunkShared to enable faces to group by chunk for culling
namespace Oasis.Core
{
    // [UpdateInGroup(typeof(SliceGroup))]
    
    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class FaceChunkSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem ecbSystem;
        
        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAll<FaceChunk>()
                .ForEach((Entity e, int entityInQueryIndex, in FaceChunk faceChunk) =>
                {
                    ecb.AddSharedComponent(entityInQueryIndex, e, new FaceChunkShared{Value = faceChunk.Value});
                    ecb.RemoveComponent<FaceChunk>(entityInQueryIndex, e);
                })
                .WithoutBurst()
                .Schedule();

            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

    }
}