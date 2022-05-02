using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{

    public partial class CubeCreate : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter(); 
            Entities
                .WithAll<LoadedDependenciesTag,CreateSlices>()
                .ForEach((Entity e, int entityInQueryIndex, in CubeInstance cubeInstance) =>
                {
                    for (var axis = 0; axis < 3; axis++)
                    for (var depth = -1; depth < 1; depth++)
                    {
                        SliceSystem.CreateSlice(ecb, entityInQueryIndex, e, depth, axis, false, new int3(1));
                    }
                    ecb.RemoveComponent<CreateSlices>(entityInQueryIndex, e);
                })
                .Schedule();
            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
