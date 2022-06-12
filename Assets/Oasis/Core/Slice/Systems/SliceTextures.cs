using System.Threading.Tasks;
using Oasis.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(SliceGroup))]
    [UpdateAfter(typeof(SliceBlockStates))]
    public partial class SliceTextures : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var blockStates = GetComponentDataFromEntity<BlockState>(true);

            Entities
                .WithAll<ComputeTextures>()
                .ForEach((Entity e, 
                    int entityInQueryIndex, 
                    ref DynamicBuffer<SideATexture> aTexturesBuffer, ref DynamicBuffer<SideBTexture> bTexturesBuffer, 
                    in DynamicBuffer<SideABlockState> sideABlockStates, in DynamicBuffer<SideBBlockState> sideBBlockStates, 
                    in Parent parent, in Slice slice) =>
                {
                    var aTextures = aTexturesBuffer.Reinterpret<Entity>();
                    var bTextures = bTexturesBuffer.Reinterpret<Entity>();

                    // Slice has liquid?
                    var aHasLiquid = false;
                    var bHasLiquid = false;
                    
                    for (var i = 0; i < slice.Size(); i++)
                    {
                        var aBlockState = blockStates[sideABlockStates[i].Value];
                        var bBlockState = blockStates[sideBBlockStates[i].Value];

                        var aTexture = Entity.Null;
                        var bTexture = Entity.Null;

                        var aIsOpaqueCube = aBlockState.blockType == BlockType.cube && aBlockState.textureType == TextureType.Opaque;
                        var aIsTransCube = aBlockState.blockType == BlockType.cube && aBlockState.textureType == TextureType.Transparent;
                        var aIsAlphaClipCube = aBlockState.blockType == BlockType.cube && aBlockState.textureType == TextureType.AlphaClip;
                        var aIsLiquid = aBlockState.blockType == BlockType.liquid;
                    
                        var bIsOpaqueCube = bBlockState.blockType == BlockType.cube && bBlockState.textureType == TextureType.Opaque;
                        var bIsTransCube = bBlockState.blockType == BlockType.cube && bBlockState.textureType == TextureType.Transparent;
                        var bIsAlphaClipCube = bBlockState.blockType == BlockType.cube && bBlockState.textureType == TextureType.AlphaClip;
                        var bIsLiquid = bBlockState.blockType == BlockType.liquid;

                        aHasLiquid = (aHasLiquid || aIsLiquid);
                        bHasLiquid = (bHasLiquid || bIsLiquid);
                        
                        // Front faces
                        if ((aIsOpaqueCube && !bIsOpaqueCube) ||
                            (aIsTransCube && !bIsTransCube && !bIsOpaqueCube) ||
                            (aIsLiquid && !bIsLiquid && !bIsOpaqueCube && !bBlockState.waterlogged) ||
                            (aIsAlphaClipCube && !bIsAlphaClipCube && !bIsOpaqueCube))
                        {
                            Side side = (Side)(slice.Axis % 6);
                            aTexture = ComputeTexture(aBlockState, side);
                        }
                        
                        // Back faces
                        if ((bIsOpaqueCube && !aIsOpaqueCube) ||
                            (bIsTransCube && !aIsTransCube && !aIsOpaqueCube) ||
                            (bIsLiquid && !aIsLiquid && !aIsOpaqueCube && !aBlockState.waterlogged) ||
                            (bIsAlphaClipCube && !aIsAlphaClipCube && !aIsOpaqueCube))
                        {
                            Side side = (Side)(3+slice.Axis % 6);
                            bTexture = ComputeTexture(bBlockState, side);
                        }
                        
                        aTextures.Add(aTexture);
                        bTextures.Add(bTexture);
                    }

                    if (aHasLiquid || bHasLiquid)
                        ecb.AddComponent(entityInQueryIndex, e, new HasLiquidTag{SideA = aHasLiquid, SideB = bHasLiquid});
                        
                    ecb.RemoveComponent<ComputeTextures>(entityInQueryIndex, e);
                })
                .WithReadOnly(blockStates)
                .ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(this.Dependency);
        }


        private static Entity ComputeTexture(BlockState blockState, Side side)
        {
            if (blockState.blockType == BlockType.liquid)
                return blockState.still;
                
            return side switch
            {
                // TODO rotation
                Side.Up => blockState.up,
                Side.Down => blockState.down,
                Side.North => blockState.north,
                Side.South => blockState.south,
                Side.East => blockState.east,
                Side.West => blockState.west,
                _ => blockState.up
            };
            
        }
    }
}