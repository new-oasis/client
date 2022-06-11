using System;
using System.Threading.Tasks;
using Oasis.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(SliceGroup))]
    [UpdateAfter(typeof(SliceTextures))]
    public partial class SliceFaces : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
    
        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var textures = GetComponentDataFromEntity<Texture>(true);
            var blockStates = GetComponentDataFromEntity<BlockState>(true);

            var opaqueUnlit = GetSingleton<PrefabFaceOpaqueUnlit>().Value;
            var opaqueLit = GetSingleton<PrefabFaceOpaqueLit>().Value;
            var transparentLit = GetSingleton<PrefabFaceTransparentLit>().Value;
            var transparentUnlit = GetSingleton<PrefabFaceTransparentUnlit>().Value;
            var alphaClipLit = GetSingleton<PrefabFaceAlphaClipLit>().Value;
            var alphaClipUnlit = GetSingleton<PrefabFaceAlphaClipUnlit>().Value;
            
            Entities
                .WithAll<ComputeFaces>()
                .ForEach((Entity e, int entityInQueryIndex,
                    ref DynamicBuffer<SideATexture> sideATextures, ref DynamicBuffer<SideBTexture> sideBTextures,
                    in DynamicBuffer<SideABlockState> sideABlockStates, in DynamicBuffer<SideBBlockState> sideBBlockStates,
                    in Parent parent, in Slice slice) =>
                {
                    // LOOP BOTH SIDES 
                    for (var n = 0; n < 2; n++)
                    {
                        var sideTextures = (n == 0) ? sideATextures.Reinterpret<Entity>() : sideBTextures.Reinterpret<Entity>();
                        var sideBlockStates = (n == 0) ? sideABlockStates.Reinterpret<Entity>() : sideBBlockStates.Reinterpret<Entity>();
                        
                        var u = (slice.Axis + 1) % 3; // => [1, 2, 0]
                        var v = (slice.Axis + 2) % 3; // => [2, 0, 1]
                        var i = 0;
                        var current = new NativeArray<int>(3, Allocator.Temp); // int[] current = new int[3];
                        current[slice.Axis] = slice.Depth + 1;

                        for (var sliceV = 0; sliceV < slice.Dims[v]; ++sliceV)
                        {
                            for (var sliceU = 0; sliceU < slice.Dims[u];)
                            {
                                var sideTexture = sideTextures[i];
                                var sideBlockState = sideBlockStates[i];
                                                                         
                                // If non-liquid texture 
                                if (!sideTexture.Equals(Entity.Null) && blockStates[sideBlockState].blockType != BlockType.liquid)
                                {
                                    int width;
                                    for (width = 1;
                                         (sliceU + width < slice.Dims[u]) && (sideTexture == (sideTextures[i + width]));
                                         ++width)
                                    {
                                    }
                                                                         
                                    bool done = false;
                                    int height, k;
                                    for (height = 1; sliceV + height < slice.Dims[v]; ++height)
                                    {
                                        for (k = 0; k < width; ++k)
                                        {
                                            if ((sideTexture != sideTextures[i + k + (height * slice.Dims[u])]))
                                            {
                                                done = true;
                                                break;
                                            }
                                        }
                                                                         
                                        if (done) break;
                                    }
                                                                         
                                    current[u] = sliceU;
                                    current[v] = sliceV;
                                    Texture texture = textures[sideTexture];
                                    Entity meshEntity;
                                                                         
                                    if (texture.type == TextureType.Opaque && !slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, opaqueUnlit);
                                    else if (texture.type == TextureType.Opaque && slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, opaqueLit);
                                    else if (texture.type == TextureType.Transparent && !slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, transparentUnlit);
                                    else if (texture.type == TextureType.Transparent && slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, transparentLit);
                                    else if (texture.type == TextureType.AlphaClip && !slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, alphaClipUnlit);
                                    else if (texture.type == TextureType.AlphaClip && slice.Lit)
                                        meshEntity = ecb.Instantiate(entityInQueryIndex, alphaClipLit);
                                    else
                                        throw new Exception("No prefab found");
                                    ecb.AddComponent<FaceTag>(entityInQueryIndex, meshEntity);
                                                                         
                                    // Ref from face to chunk.  FaceChunkSystem will group faces by chunk using SharedComponent for culling
                                    ecb.AddComponent<FaceChunk>(entityInQueryIndex, meshEntity, new FaceChunk() {Value = parent.Value});
                                    
                                    // Parent and LTP
                                    ecb.AddComponent(entityInQueryIndex, meshEntity, new Parent {Value = e}); // Parent under slice
                                    ecb.AddComponent(entityInQueryIndex, meshEntity, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
                                                                         
                                    // TRANSLATION, ROTATION, SCALE
                                    Side side = (Side)((n*3)+slice.Axis % 6);  //n*3 means +3 for backside
                                    if (side == Side.East)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(0, 270f, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = height});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = width});
                                    }
                                    else if (side == Side.Up)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(90f, 0, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = height});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = width});
                                    }
                                    else if (side == Side.North)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(width + current[0], current[1], current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(0, 180f, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(width, height, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = width});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = height});
                                    }
                                    else if (side == Side.West)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(current[0], current[1], height + current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(0, 90f, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = height});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = width});
                                    }
                                    else if (side == Side.Down)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(height + current[0], current[1], current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(-90f, 180f, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = height});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = width});
                                    }
                                    else if (side == Side.South)
                                    {
                                        ecb.SetComponent(entityInQueryIndex, meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new Rotation {Value = Quaternion.Euler(0, 0, 0)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new NonUniformScale {Value = new float3(width, height, 1f)});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileU {Value = width});
                                        ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderTileV {Value = height});
                                    }
                                                                         
                                    // Shader params; array index and tiling xy
                                    ecb.AddComponent(entityInQueryIndex, meshEntity, new ShaderArrayIndex {Value = texture.index});
                                                                         
                                    // Zero-out face elements
                                    for (var l = 0; l < height; ++l)
                                        for (k = 0; k < width; ++k)
                                            sideTextures[i + k + l * slice.Dims[u]] = Entity.Null;
                                                                         
                                    // Increment counters and continue
                                    sliceU += width;
                                    i += width;
                                }
                                else
                                {
                                    ++sliceU;
                                    ++i;
                                }
                            }
                        }
                    }
                
                    ecb.RemoveComponent<ComputeFaces>(entityInQueryIndex, e);
                })
                .WithReadOnly(blockStates)
                .WithReadOnly(textures)
                .ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

    }
}

                 
// Culling
// var id = new uint4(1, 0, 0, 0); // TODO should be chunk id?
// ecb.AddSharedComponent<FrozenRenderSceneTag>(entityInQueryIndex, meshEntity, new FrozenRenderSceneTag() { SceneGUID = new Hash128 { Value = id } });
// ecb.AddComponent<PerInstanceCullingTag>(entityInQueryIndex, meshEntity);