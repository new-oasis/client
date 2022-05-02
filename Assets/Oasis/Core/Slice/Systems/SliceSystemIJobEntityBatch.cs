// using System;
// using Unity.Entities;
// using Unity.Transforms;
// using Unity.Physics;
// using Oasis.Core;
// using Unity.Collections;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Profiling;
// using UnityEngine;
// using Texture = Oasis.Grpc.Texture;
//
// public class SliceSystemNew : SystemBase
// {
//
//     static readonly ProfilerMarker k_BlockStates = new ProfilerMarker("SliceSystem.BlockStates");
//     static readonly ProfilerMarker k_Textures = new ProfilerMarker("SliceSystem.Textures");
//     static readonly ProfilerMarker k_Faces = new ProfilerMarker("SliceSystem.Textures");
//
//
//     void BlockStates()
//     {
//         var NewBlockStatesQuery = GetEntityQuery(new EntityQueryDesc
//         {
//             All = new ComponentType[] {ComponentType.ReadOnly<Slice>(), ComponentType.ReadOnly<Parent>(),},
//             None = new ComponentType[] {typeof(SideABlockState), typeof(SideBBlockState)}
//         });
//         
//         // Add slice blockstates outside job
//         EntityManager.AddComponent(NewBlockStatesQuery, typeof(SideABlockState));
//         EntityManager.AddComponent(NewBlockStatesQuery, typeof(SideBBlockState));
//         
//         // Prepare job
//         var blockStateJob = new BlockStateJob();
//         blockStateJob.Chunks = GetComponentDataFromEntity<Chunk>(true);
//         blockStateJob.Voxels = GetBufferFromEntity<VoxelElement>(true);
//         blockStateJob.BlockStates = GetBufferFromEntity<BlockStateElement>(true);
//         blockStateJob.Air = World.GetOrCreateSystem<BlockStates>().Air;
//         
//         blockStateJob.ParentsHandle = GetComponentTypeHandle<Parent>(true);
//         blockStateJob.SideABlockStatesHandle = GetBufferTypeHandle<SideABlockState>(true);
//         blockStateJob.SideBBlockStatesHandle = GetBufferTypeHandle<SideBBlockState>(true);
//         
//         // Do Job
//         var jobHandle = blockStateJob.ScheduleParallel(NewBlockStatesQuery, 1, this.Dependency);
//         jobHandle.Complete();
//     }
//
//     public struct BlockStateJob : IJobEntityBatch
//     {
//         // Chunk components
//         public NativeHashMap<int3, Entity> ChunkEntities;
//         public ComponentDataFromEntity<Chunk> Chunks;
//         public ComponentDataFromEntity<LoadedDependenciesTag> LoadedDependencies;
//         public BufferFromEntity<VoxelElement> Voxels;
//         public BufferFromEntity<BlockStateElement> BlockStates;
//         public Entity Air;
//
//         // Slice components
//         public ComponentTypeHandle<Parent> ParentsHandle;
//         public  BufferTypeHandle<SideABlockState> SideABlockStatesHandle;
//         public  BufferTypeHandle<SideBBlockState> SideBBlockStatesHandle;
//         public  Slice slice;
//             
//         public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
//         {
//             // Get accessor to buffers in chunk
//             var parents = batchInChunk.GetNativeArray(ParentsHandle);
//             var sideABlockStatesBuffers = batchInChunk.GetBufferAccessor(SideABlockStatesHandle);
//             var sideBBlockStatesBuffers = batchInChunk.GetBufferAccessor(SideBBlockStatesHandle);
//             
//                 
//             // Loop through entities in chunk
//             for (int c = 0; c < batchInChunk.Count; c++)
//             {
//                 var parent = parents[c].Value;
//                 var isChunk = Chunks.HasComponent(parent);
//                 
//                 // Get Chunk voxels
//                 if (!Voxels.HasComponent(parent))
//                     return;
//                 
//                 
//                 // Get actual buffer for entity
//                 var aBlockStates = sideABlockStatesBuffers[c].Reinterpret<Entity>();
//                 var bBlockStates = sideBBlockStatesBuffers[c].Reinterpret<Entity>();
//
//                 // Voxel for looping across slice
//                 var voxel = new NativeArray<int>(3, Allocator.Temp); // int[] voxel = new int[3] { 0, 0, 0 };
//                 voxel[slice.Axis] = slice.Depth;
//                 var u = (slice.Axis + 1) % 3; // => [1, 2, 0]
//                 var v = (slice.Axis + 2) % 3; // => [2, 0, 1]
//                 var offset = new NativeArray<int>(3, Allocator.Temp); // int[] offset = new int[3] { 0, 0, 0 };
//                 offset[slice.Axis] = 1;
//
//                 // loop through uv
//                 var n = 0;
//                 var voxels = Voxels[parent];
//                 var paletteItemEntity = BlockStates[parent];
//                 for (voxel[v] = 0; voxel[v] < slice.Dims[v]; ++voxel[v])
//                 {
//                     for (voxel[u] = 0; voxel[u] < slice.Dims[u]; ++voxel[u], ++n)
//                     {
//                         var offset3 = new int3(offset[0], offset[1], offset[2]);
//
//                         var thisChunk = isChunk ? Chunks[parent].id : new int3();
//                         var aChunk = (slice.Depth == -1) ? (thisChunk - offset3) : thisChunk;
//                         var aVoxel = new int3(voxel[0], voxel[1], voxel[2]);
//                         var aWorldVoxel = aVoxel + aChunk * 16;
//                         var aIndex = aWorldVoxel.ChunkVoxel().ToIndex(slice.Dims);
//
//                         var bWorldVoxel = aWorldVoxel + offset3;
//                         var bChunk = bWorldVoxel.Chunk();
//                         var bIndex = bWorldVoxel.ChunkVoxel().ToIndex(slice.Dims);
//
//                         Entity aBlockState;
//                         Entity bBlockState;
//
//                         // Edge
//                         if (slice.Depth == -1)
//                         {
//                             if (isChunk)
//                             {
//                                 var aChunkEntity = ChunkEntities.ContainsKey(aChunk) &&
//                                                    LoadedDependencies.HasComponent(ChunkEntities[bChunk])
//                                     ? ChunkEntities[aChunk]
//                                     : Entity.Null; // Get chunk with a
//                                 if (Voxels.HasComponent(aChunkEntity) &&
//                                     Voxels[aChunkEntity].Length == 4096 &&
//                                     BlockStates.HasComponent(aChunkEntity))
//                                 {
//                                     var paletteId = Voxels[aChunkEntity][aIndex].Value;
//                                     aBlockState = BlockStates[aChunkEntity][paletteId].Value;
//                                 }
//                                 else
//                                     aBlockState = Air;
//                             }
//                             else
//                                 aBlockState = Air;
//
//                             var bPaletteId = voxels[bIndex].Value;
//                             bBlockState = paletteItemEntity[bPaletteId].Value;
//                         }
//
//                         // Other edge
//                         else if (slice.Depth == (slice.Dims[slice.Axis] - 1))
//                         {
//                             if (isChunk)
//                             {
//                                 Entity bChunkEntity = ChunkEntities.ContainsKey(bChunk) &&
//                                                       LoadedDependencies.HasComponent(ChunkEntities[bChunk])
//                                     ? ChunkEntities[bChunk]
//                                     : Entity.Null; // Get chunk with b
//                                 if (Voxels.HasComponent(bChunkEntity) &&
//                                     Voxels[bChunkEntity].Length == 4096 &&
//                                     BlockStates.HasComponent(bChunkEntity))
//                                 {
//                                     var paletteId = Voxels[bChunkEntity][bIndex].Value;
//                                     bBlockState = BlockStates[bChunkEntity][paletteId].Value;
//                                 }
//                                 else
//                                     bBlockState = Air;
//                             }
//                             else
//                                 bBlockState = Air;
//
//                             var aPaletteId = voxels[aIndex].Value;
//                             aBlockState = paletteItemEntity[aPaletteId].Value;
//                         }
//
//                         // Not edge
//                         else
//                         {
//                             var aPaletteId = voxels[aIndex].Value;
//                             var bPaletteId = voxels[bIndex].Value;
//                             aBlockState = paletteItemEntity[aPaletteId].Value;
//                             bBlockState = paletteItemEntity[bPaletteId].Value;
//                         }
//
//                         if (aBlockState == Entity.Null)
//                             Debug.LogError(
//                                 $"ablockstate:{aBlockState} v:{v} u:{u} slice.depth:{slice.Depth} isChunk:{isChunk}");
//                         if (bBlockState == Entity.Null)
//                             Debug.LogError(
//                                 $"bblockstate:{bBlockState} v:{v} u:{u} slice.depth:{slice.Depth} isChunk:{isChunk}");
//
//                         aBlockStates.Add(aBlockState);
//                         bBlockStates.Add(bBlockState);
//                     }
//                 }
//             }
//         }
//     }
//
//
//     void Textures()
//     {
//         var NewTexturesQuery = GetEntityQuery(new EntityQueryDesc
//         {
//             All = new ComponentType[] {ComponentType.ReadOnly<SideABlockState>(), ComponentType.ReadOnly<SideBBlockState>(), ComponentType.ReadOnly<Parent>(), ComponentType.ReadOnly<Slice>(), },
//             None = new ComponentType[] {typeof(SideATexture), typeof(SideBTexture)}
//         });
//         
//         // Add slice textures outside job
//         EntityManager.AddComponent(NewTexturesQuery, typeof(SideATexture));
//         EntityManager.AddComponent(NewTexturesQuery, typeof(SideBTexture));
//         
//         // Prepare job
//         var texturesJob = new TexturesJob();
//         texturesJob.BlockStates = GetComponentDataFromEntity<BlockState>(true);
//         texturesJob.Blocks = GetComponentDataFromEntity<Block>(true);
//         texturesJob.Models = GetComponentDataFromEntity<Model>(true);
//         texturesJob.SideABlockStatesHandle = GetBufferTypeHandle<SideABlockState>(true);
//         texturesJob.SideBBlockStatesHandle = GetBufferTypeHandle<SideBBlockState>(true);
//         texturesJob.SideATexturesHandle = GetBufferTypeHandle<SideATexture>(true);
//         texturesJob.SideBTexturesHandle = GetBufferTypeHandle<SideBTexture>(true);
//
//         // Do Job
//         var jobHandle = texturesJob.ScheduleParallel(NewTexturesQuery, 1, this.Dependency);
//         jobHandle.Complete();
//     }
//     
//     public struct TexturesJob : IJobEntityBatch
//     {
//         public ComponentDataFromEntity<BlockState> BlockStates;
//         public ComponentDataFromEntity<Block> Blocks;
//         public ComponentDataFromEntity<Model> Models;
//         
//         // Slice components
//         public BufferTypeHandle<SideABlockState> SideABlockStatesHandle;
//         public BufferTypeHandle<SideBBlockState> SideBBlockStatesHandle;
//         public BufferTypeHandle<SideATexture> SideATexturesHandle;
//         public BufferTypeHandle<SideBTexture> SideBTexturesHandle;
//         public Slice slice;
//
//         public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
//         {
//             var aTexturesBuffers = batchInChunk.GetBufferAccessor(SideATexturesHandle);
//             var bTexturesBuffers = batchInChunk.GetBufferAccessor(SideBTexturesHandle);
//             var sideABlockStatesBuffers = batchInChunk.GetBufferAccessor(SideABlockStatesHandle);
//             var sideBBlockStatesBuffers = batchInChunk.GetBufferAccessor(SideBBlockStatesHandle);
//             for (int c = 0; c < batchInChunk.Count; c++)
//             {
//                 var aTextures = aTexturesBuffers[c].Reinterpret<Entity>();
//                 var bTextures = bTexturesBuffers[c].Reinterpret<Entity>();
//                 var aBlockStates = sideABlockStatesBuffers[c].Reinterpret<Entity>();
//                 var bBlockStates = sideBBlockStatesBuffers[c].Reinterpret<Entity>();
//
//                 var aHasLiquid = false;
//                 var bHasLiquid = false;
//
//                 for (var i = 0; i < slice.Size(); i++)
//                 {
//                     var aBlockState = BlockStates[aBlockStates[i]];
//                     var bBlockState = BlockStates[bBlockStates[i]];
//
//                     var aBlock = Blocks[aBlockState.block];
//                     var bBlock = Blocks[bBlockState.block];
//
//                     var aTexture = Entity.Null;
//                     var bTexture = Entity.Null;
//
//                     var aIsOpaqueCube = aBlock.blockType == BlockType.cube && aBlock.textureType == TextureType.Opaque;
//                     var aIsTransCube = aBlock.blockType == BlockType.cube && aBlock.textureType == TextureType.Transparent;
//                     var aIsAlphaClipCube =
//                         aBlock.blockType == BlockType.cube && aBlock.textureType == TextureType.AlphaClip;
//                     var aIsLiquid = aBlock.blockType == BlockType.liquid;
//
//                     var bIsOpaqueCube = bBlock.blockType == BlockType.cube && bBlock.textureType == TextureType.Opaque;
//                     var bIsTransCube = bBlock.blockType == BlockType.cube && bBlock.textureType == TextureType.Transparent;
//                     var bIsAlphaClipCube =
//                         bBlock.blockType == BlockType.cube && bBlock.textureType == TextureType.AlphaClip;
//                     var bIsLiquid = bBlock.blockType == BlockType.liquid;
//
//                     aHasLiquid = (aHasLiquid || aIsLiquid);
//                     bHasLiquid = (bHasLiquid || bIsLiquid);
//
//                     // Front faces
//                     if ((aIsOpaqueCube && !bIsOpaqueCube) ||
//                         (aIsTransCube && !bIsTransCube && !bIsOpaqueCube) ||
//                         (aIsLiquid && !bIsLiquid && !bIsOpaqueCube) ||
//                         (aIsAlphaClipCube && !bIsOpaqueCube))
//                     {
//                         Side side = (Side) (slice.Axis % 6);
//                         aTexture = ComputeTexture(Models[aBlockState.model], side);
//                     }
//
//                     // Back faces
//                     if ((bIsOpaqueCube && !aIsOpaqueCube) ||
//                         (bIsTransCube && !aIsTransCube && !aIsOpaqueCube) ||
//                         (bIsLiquid && !aIsLiquid && !aIsOpaqueCube) ||
//                         (bIsAlphaClipCube && !aIsOpaqueCube))
//                     {
//                         Side side = (Side) (3 + slice.Axis % 6);
//                         bTexture = ComputeTexture(Models[bBlockState.model], side);
//                     }
//
//                     aTextures.Add(aTexture);
//                     bTextures.Add(bTexture);
//                 }
//
//                 // if (aHasLiquid || bHasLiquid)
//                     // EntityManager.AddComponentData(e, new HasLiquidTag {SideA = aHasLiquid, SideB = bHasLiquid});
//             }
//         }
//     }
//     private static Entity ComputeTexture(Model model, Side side)
//     {
//         return side switch
//         {
//             // TODO rotation
//             Side.Up => model.up,
//             Side.Down => model.down,
//             Side.North => model.north,
//             Side.South => model.south,
//             Side.East => model.east,
//             Side.West => model.west,
//             _ => model.up
//         };
//     }
//
//
//     
//     void Faces()
//     {
//         var textures = GetComponentDataFromEntity<Oasis.Core.Texture>(true);
//         var blockStates = GetComponentDataFromEntity<BlockState>(true);
//      
//         var opaqueUnlit = GetSingleton<PrefabFaceOpaqueUnlit>().Value;
//         var opaqueLit = GetSingleton<PrefabFaceOpaqueLit>().Value;
//         var transparentLit = GetSingleton<PrefabFaceTransparentLit>().Value;
//         var transparentUnlit = GetSingleton<PrefabFaceTransparentUnlit>().Value;
//         var alphaClipLit = GetSingleton<PrefabFaceAlphaClipLit>().Value;
//         var alphaClipUnlit = GetSingleton<PrefabFaceAlphaClipUnlit>().Value;
//
//         var jobHandle = new JobHandle();
//         Entities
//             .WithNone<Child>()
//             .ForEach((Entity e, int entityInQueryIndex,
//                 ref DynamicBuffer<SideATexture> sideATextures,
//                 ref DynamicBuffer<SideBTexture> sideBTextures,
//                 in DynamicBuffer<SideABlockState> sideABlockStates,
//                 in DynamicBuffer<SideBBlockState> sideBBlockStates,
//                 in Parent parent,
//                 in Slice slice) =>
//             {
//      
//                 // LOOP BOTH SIDES 
//                 for (var n = 0; n < 2; n++)
//                 {
//                     var sideTextures = (n == 0) ? sideATextures.Reinterpret<Entity>() : sideBTextures.Reinterpret<Entity>();
//                     var sideBlockStates = (n == 0) ? sideABlockStates.Reinterpret<Entity>() : sideBBlockStates.Reinterpret<Entity>();
//                              
//                     var u = (slice.Axis + 1) % 3; // => [1, 2, 0]
//                     var v = (slice.Axis + 2) % 3; // => [2, 0, 1]
//                     var i = 0;
//                     var current = new NativeArray<int>(3, Allocator.Temp); // int[] current = new int[3];
//                     current[slice.Axis] = slice.Depth + 1;
//      
//                     for (var sliceV = 0; sliceV < slice.Dims[v]; ++sliceV)
//                     {
//                         for (var sliceU = 0; sliceU < slice.Dims[u];)
//                         {
//                             var sideTexture = sideTextures[i];
//                             var sideBlockState = sideBlockStates[i];
//                                                                               
//                             // If texture and not liquid;  TODO optimize liquid check
//                             if (!sideTexture.Equals(Entity.Null) && !blockStates[sideBlockState].liquid)
//                             {
//                                 int width;
//                                 for (width = 1;
//                                      (sliceU + width < slice.Dims[u]) && (sideTexture == (sideTextures[i + width]));
//                                      ++width)
//                                 {
//                                 }
//                                                                               
//                                 bool done = false;
//                                 int height, k;
//                                 for (height = 1; sliceV + height < slice.Dims[v]; ++height)
//                                 {
//                                     for (k = 0; k < width; ++k)
//                                     {
//                                         if ((sideTexture != sideTextures[i + k + (height * slice.Dims[u])]))
//                                         {
//                                             done = true;
//                                             break;
//                                         }
//                                     }
//                                                                               
//                                     if (done) break;
//                                 }
//                                                                               
//                                 current[u] = sliceU;
//                                 current[v] = sliceV;
//                                 Oasis.Core.Texture texture = textures[sideTexture];
//                                 Entity meshEntity;
//                                                                               
//                                 if (texture.type == TextureType.Opaque && !slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(opaqueUnlit);
//                                 else if (texture.type == TextureType.Opaque && slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(opaqueLit);
//                                 else if (texture.type == TextureType.Transparent && !slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(transparentUnlit);
//                                 else if (texture.type == TextureType.Transparent && slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(transparentLit);
//                                 else if (texture.type == TextureType.AlphaClip && !slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(alphaClipUnlit);
//                                 else if (texture.type == TextureType.AlphaClip && slice.Lit)
//                                     meshEntity = EntityManager.Instantiate(alphaClipLit);
//                                 else
//                                     throw new Exception("No prefab found");
//                                 EntityManager.AddComponent<FaceTag>(meshEntity); 
//                                                                               
//                                 // Parent and LTP
//                                 EntityManager.AddComponentData(meshEntity, new Parent {Value = e}); // Parent under slice
//                                 EntityManager.AddComponentData(meshEntity, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
//                                                                               
//                                 // TRANSLATION, ROTATION, SCALE
//                                 Side side = (Side)((n*3)+slice.Axis % 6);  //n*3 means +3 for backside
//                                 if (side == Side.East)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(0, 270f, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = height});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = width});
//                                 }
//                                 else if (side == Side.Up)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(90f, 0, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = height});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = width});
//                                 }
//                                 else if (side == Side.North)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(width + current[0], current[1], current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(0, 180f, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(width, height, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = width});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = height});
//                                 }
//                                 else if (side == Side.West)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(current[0], current[1], height + current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(0, 90f, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = height});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = width});
//                                 }
//                                 else if (side == Side.Down)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(height + current[0], current[1], current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(-90f, 180f, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(height, width, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = height});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = width});
//                                 }
//                                 else if (side == Side.South)
//                                 {
//                                     EntityManager.SetComponentData(meshEntity, new Translation {Value = new int3(current[0], current[1], current[2])});
//                                     EntityManager.AddComponentData(meshEntity, new Rotation {Value = Quaternion.Euler(0, 0, 0)});
//                                     EntityManager.AddComponentData(meshEntity, new NonUniformScale {Value = new float3(width, height, 1f)});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileU {Value = width});
//                                     EntityManager.AddComponentData(meshEntity, new ShaderTileV {Value = height});
//                                 }
//                                                                               
//                                 // Shader params; array index and tiling xy
//                                 EntityManager.AddComponentData(meshEntity, new ShaderArrayIndex {Value = texture.index});
//                                                                               
//                                 // Zero-out face elements
//                                 for (var l = 0; l < height; ++l)
//                                 for (k = 0; k < width; ++k)
//                                     sideTextures[i + k + l * slice.Dims[u]] = Entity.Null;
//                                                                               
//                                 // Increment counters and continue
//                                 sliceU += width;
//                                 i += width;
//                             }
//                             else
//                             {
//                                 ++sliceU;
//                                 ++i;
//                             }
//                         }
//                     }
//                 }
//                      
//             })
//             .WithReadOnly(blockStates)
//             .WithReadOnly(textures)
//             .ScheduleParallel(jobHandle);
//
//         jobHandle.Complete();
//     }
//  
//     protected override void OnUpdate()
//     {
//         k_BlockStates.Begin();
//         BlockStates();
//         k_BlockStates.End();
//
//         k_Textures.Begin();
//         Textures();
//         k_Textures.End();
//
//         k_Faces.Begin();
//         Faces();
//         k_Faces.End();
//     }
//     
//    
// }