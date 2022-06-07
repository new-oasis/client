using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    
    [UpdateInGroup(typeof(ChunkGroup))]
    [UpdateAfter(typeof(ChunkDependencies))]
    public partial class ChunkSlices : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem ecbSystem;
        private int count;

        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter(); 
            var chunkEntities = World.GetOrCreateSystem<ChunkSystem>()._entities;
            var childBuffers = GetBufferFromEntity<Child>(false);
            
            Entities
                .WithAll<LoadedDependenciesTag, CreateSlices>()
                .ForEach((Entity e, int entityInQueryIndex, in Chunk chunk, in VisibleTag visibleTag) =>
                {
                    Debug.Log("Creating Slice");
                    int3 dims = new int3(16);
                    for (int axis = 0; axis < 3; axis++)
                    for (int depth = 0; depth < 16; depth++)
                        SliceSystem.CreateSlice(ecb, entityInQueryIndex, e, depth, axis, visibleTag.lit);
                    
                    
                    // Create edge slices in adjacent chunks
                    // Down
                    int3 downId = chunk.id + new int3(0, -1, 0);
                    Entity down = chunkEntities.ContainsKey(downId) ? chunkEntities[downId] : Entity.Null;
                    if (HasComponent<LoadedDependenciesTag>(down) && childBuffers.HasComponent(down))
                    {
                        var slice = SliceSystem.CreateSlice(ecb, entityInQueryIndex, down, 15, 1, visibleTag.lit);
                        ecb.AddComponent<DupeCheck>(entityInQueryIndex, slice); 
                    }

                    // South
                    int3 southId = chunk.id + new int3(0, 0, -1);
                    Entity south = chunkEntities.ContainsKey(southId) ? chunkEntities[southId] : Entity.Null;
                    if (HasComponent<LoadedDependenciesTag>(south) && childBuffers.HasComponent(south))
                    {
                        var slice = SliceSystem.CreateSlice(ecb, entityInQueryIndex, south, 15, 2, visibleTag.lit);
                        ecb.AddComponent<DupeCheck>(entityInQueryIndex, slice);
                    }
                    
                    
                    // West
                    int3 westId = chunk.id + new int3(-1, 0, 0);
                    Entity west = chunkEntities.ContainsKey(westId) ? chunkEntities[westId] : Entity.Null;
                    if (HasComponent<LoadedDependenciesTag>(west) && childBuffers.HasComponent(west))
                    {
                        var slice = SliceSystem.CreateSlice(ecb, entityInQueryIndex, west, 15, 0, visibleTag.lit);
                        ecb.AddComponent<DupeCheck>(entityInQueryIndex, slice);
                    }
                    
                    ecb.RemoveComponent<CreateSlices>(entityInQueryIndex, e);
                })
                .WithNativeDisableParallelForRestriction(childBuffers)
                .WithReadOnly(chunkEntities)
                .ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }



        // static void CreateSlice(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity parent, int depth, int axis, bool lit)
        // {
        //     var slice = ecb.CreateEntity(entityInQueryIndex);
        //     // ecb.AddComponent(entityInQueryIndex, slice, typeof(ComputeBlockStates));
        //     ecb.AddComponent(entityInQueryIndex, slice, new Slice {Depth = depth, Axis = axis, Dims = new int3(16), Lit = lit});
        //     ecb.AddComponent(entityInQueryIndex, slice, new Parent {Value = parent});
        //     ecb.AddComponent(entityInQueryIndex, slice, new LocalToWorld { });
        //     ecb.AddComponent(entityInQueryIndex, slice, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
        // }
        //
        
        
    }
}
