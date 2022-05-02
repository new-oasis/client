using System.Threading.Tasks;
using Unity.Entities;

namespace Oasis.Core
{
    
    [UpdateInGroup(typeof(ChunkGroup))]
    [UpdateAfter(typeof(ChunkSystem))]
    public partial class ChunkDependencies : SystemBase
    {
    
        EndInitializationEntityCommandBufferSystem _ecbSystem;
    
        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            base.OnCreate();
        }
    
        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithNone<LoadedDependenciesTag>()
                .ForEach((Entity e, int entityInQueryIndex, in Chunk chunk, in DynamicBuffer<BlockStateElement> blockstates) =>
                {

                    bool loaded = true;
                    for (int i = 0; i < blockstates.Length; i++)
                        loaded = loaded && HasComponent<LoadedDependenciesTag>(blockstates[i].Value);
                    
                    if (loaded)
                        ecb.AddComponent<LoadedDependenciesTag>(entityInQueryIndex, e);

                }).ScheduleParallel();
        
            _ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}