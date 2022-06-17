using System;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
// using System.Linq;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(SliceGroup))]
    [UpdateAfter(typeof(SliceTextures))]
    public partial class SliceLiquidCorners : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
    
        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem =
                World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var chunks = GetComponentDataFromEntity<Chunk>(true);
            var blockStates = GetComponentDataFromEntity<BlockState>(true);
            var voxelBuffers = GetBufferFromEntity<VoxelElement>(isReadOnly: true);
            var blockStateBuffers = GetBufferFromEntity<BlockStateElement>(isReadOnly: true);

            Entities
                .WithAll<HasLiquidTag>()
                .ForEach((Entity e, int entityInQueryIndex,
                    in DynamicBuffer<SideABlockState> sideABlockStates,
                    in DynamicBuffer<SideBBlockState> sideBBlockStates,
                    in Parent parent,
                    in Slice slice) =>
                {
                    var isChunk = chunks.HasComponent(parent.Value);
                    var parentVoxels = voxelBuffers[parent.Value];
                    var parentBlockStates = blockStateBuffers[parent.Value];
                    
                    var aLiquidCornersBuffer = ecb.AddBuffer<SideACorners>(entityInQueryIndex, e);
                    var bLiquidCornersBuffer = ecb.AddBuffer<SideBCorners>(entityInQueryIndex, e);
                    var aCorners = aLiquidCornersBuffer.Reinterpret<float4>();
                    var bCorners = bLiquidCornersBuffer.Reinterpret<float4>();
                    
                    for (var side = 0; side < 2; side++)
                    {
                        for (var i = 0; i < slice.Size(); i++)
                        {
                            // Get blockStates for each side
                            var corners = (side == 0) ? aCorners : bCorners;
                            var blockState = (side == 0) 
                                ? blockStates[sideABlockStates[i].Value] 
                                : blockStates[sideBBlockStates[i].Value];

                            // if not liquid.. continue  // TODO optimize this sparse array
                            if (blockState.blockType != BlockType.liquid)
                            {
                                corners.Add(new float4());
                                continue;
                            }
                            
                            var current3 = i.ToInt3(slice.Dims);
                            byte north = 0;
                            byte east = 0;
                            byte south = 0;
                            byte west = 0;
                            byte full = 8;

                            //  if north is within chunk...grab it's level
                            if (!slice.Dims.OOB(current3.North()))
                            {
                                var northIndex = current3.North().ToIndex(slice.Dims);
                                var northVoxel = parentVoxels[northIndex];
                                var northBlockStateEntity = parentBlockStates[northVoxel.Value];
                                var northBlockState = blockStates[northBlockStateEntity.Value];
                                north = northBlockState.blockType == BlockType.liquid ? (byte)(full - northBlockState.level) : (byte)0;
                            }
                            if (!slice.Dims.OOB(current3.East()))
                            {
                                var eastIndex = current3.East().ToIndex(slice.Dims);
                                var eastVoxel = parentVoxels[eastIndex];
                                var eastBlockStateEntity = parentBlockStates[eastVoxel.Value];
                                var eastBlockState = blockStates[eastBlockStateEntity.Value];
                                east = eastBlockState.blockType == BlockType.liquid ? (byte)(full - eastBlockState.level) : (byte)0;
                            }
                            if (!slice.Dims.OOB(current3.South()))
                            {
                                var southIndex = current3.South().ToIndex(slice.Dims);
                                var southVoxel = parentVoxels[southIndex];
                                var southBlockStateEntity = parentBlockStates[southVoxel.Value];
                                var southBlockState = blockStates[southBlockStateEntity.Value];
                                south = southBlockState.blockType == BlockType.liquid ? (byte)(full-southBlockState.level) : (byte)0;
                            }
                            if (!slice.Dims.OOB(current3.West()))
                            {
                                var westIndex = current3.West().ToIndex(slice.Dims);
                                var westVoxel = parentVoxels[westIndex];
                                var westBlockStateEntity = parentBlockStates[westVoxel.Value];
                                var westBlockState = blockStates[westBlockStateEntity.Value];
                                west = westBlockState.blockType == BlockType.liquid ? (byte)(full-westBlockState.level) : (byte)0;
                            }
                        
                            // Compute corners
                            var current = (byte)(8-blockState.level); // level == 0 == full; 1 == full-1
                            var northEastCorner = AverageLevel(current, north, east);
                            var northWestCorner = AverageLevel(current, north, west);
                            var southEastCorner = AverageLevel(current, south, east);
                            var southWestCorner = AverageLevel(current, south, west);
                        
                            // if top
                            corners.Add(new float4(northEastCorner, northWestCorner,
                                southWestCorner, southEastCorner));

                        }
                    }
                })
                .WithReadOnly(chunks)
                .WithReadOnly(voxelBuffers)
                .WithReadOnly(blockStateBuffers)
                .WithReadOnly(blockStates)
                // .ScheduleParallel();
                .Schedule();

            _ecbSystem.AddJobHandleForProducer(this.Dependency);
        }


        private static float AverageLevel(byte a, byte b, byte c)
        {
            var count = 0;
            var sum = 0;
            if (a != 0)
            {
                count += 1;
                sum += a;
            }
            if (b != 0)
            {
                count += 1;
                sum += b;
            }
            if (c != 0)
            {
                count += 1;
                sum += c;
            }
            if (count == 0)
                return 0;
            
            return ((float)sum / count)/8f;
        }
        
        
        [BurstDiscard]
        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}

                 





// Culling
// var id = new uint4(1, 0, 0, 0); // TODO should be chunk id?
// ecb.AddSharedComponent<FrozenRenderSceneTag>(entityInQueryIndex, meshEntity, new FrozenRenderSceneTag() { SceneGUID = new Hash128 { Value = id } });
// ecb.AddComponent<PerInstanceCullingTag>(entityInQueryIndex, meshEntity);



// Loop over slice elements
// if element is liquid..continue
// if side is top
// compute ne corner level
// Get NEW blockstates
// Average level