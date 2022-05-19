using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    
    [UpdateInGroup(typeof(ChunkGroup))]
    [UpdateAfter(typeof(ChunkSlices))]
    public partial class ChunkModels : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            RequireSingletonForUpdate<PrefabModel>();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter(); 
            Entity prefab = GetSingleton<PrefabModel>().Value;
            
            Entities
                .WithNone<ModelEntity>()
                .WithAll<LoadedDependenciesTag, VisibleTag>()
                .ForEach((Entity e, int entityInQueryIndex, in Chunk chunk, in VisibleTag visibleTag, in DynamicBuffer<BlockStateElement> blockStateElements, in DynamicBuffer<VoxelElement> voxels) =>
                {
                    ecb.AddBuffer<ModelEntity>(entityInQueryIndex, e);
                    
                    for (var x = 0; x < 16; x++)
                        for (var y = 0; y < 16; y++)
                            for (var z = 0; z < 16; z++)
                            {
                                var i = new int3(x, y, z);
                                var paletteId = voxels[i.ToIndex()].Value;
                                var blockStateEntity = blockStateElements[paletteId].Value;
                                
                                // if model then instantiate and position it.  it will create its own faces
                                var blockState = GetComponent<BlockState>(blockStateEntity);
                                if (blockState.blockType != (BlockType.model)) continue;
                                    
                                // var modelInstance = ecb.Instantiate(entityInQueryIndex, prefab);
                                // ecb.AddComponent(entityInQueryIndex, modelInstance, new Parent{Value = e});
                                // ecb.AddComponent(entityInQueryIndex, modelInstance, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
                                //
                                //
                                // ecb.AddComponent(entityInQueryIndex, modelInstance, new ModelInstance() {blockState = blockStateEntity, lit = true});
                                // ecb.AddComponent(entityInQueryIndex, modelInstance, 
                                // new Translation() {Value = new float3(x,y,z) + new float3(0.5f)});
                                //
                                // var q = quaternion.EulerXYZ(blockState.x * Mathf.Deg2Rad, blockState.y * Mathf.Deg2Rad, 0);
                                // ecb.AddComponent(entityInQueryIndex, modelInstance, new Rotation() { Value = q });
                            } 
                    
                }) .Schedule();
            
            _ecbSystem.AddJobHandleForProducer(Dependency);
        }

    }
}
