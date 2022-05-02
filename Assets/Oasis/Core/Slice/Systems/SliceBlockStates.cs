using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{

    [UpdateInGroup(typeof(SliceGroup))]
    public partial class SliceBlockStates : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;
        private ChunkSystem _chunkSystem;
        
        protected override void OnCreate()
        {
            _ecbSystem = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var voxelBuffers = GetBufferFromEntity<VoxelElement>(isReadOnly: true);
            var blockStateEntities = GetBufferFromEntity<BlockStateElement>(isReadOnly: true);
            var chunks = GetComponentDataFromEntity<Chunk>(true);

            var air = World.GetOrCreateSystem<BlockStateSystem>().Air;
            var chunkEntities = World.GetOrCreateSystem<ChunkSystem>()._entities;  // Correct use of another system?

            Entities
                .WithAll<ComputeBlockStates>()
                .ForEach((Entity e, int entityInQueryIndex,
                    ref DynamicBuffer<SideABlockState> aBlockStatesBuffer, ref DynamicBuffer<SideBBlockState> bBlockStatesBuffer,
                    in Parent parent, in Slice slice) =>
                {
                    if (!voxelBuffers.HasComponent(parent.Value))
                        return;
                    
                    var isChunk = HasComponent<Chunk>(parent.Value);
                    var aBlockStates = aBlockStatesBuffer.Reinterpret<Entity>();
                    var bBlockStates = bBlockStatesBuffer.Reinterpret<Entity>();
                    
                    // Voxel for looping across slice
                    var voxel = new NativeArray<int>(3, Allocator.Temp); // int[] voxel = new int[3] { 0, 0, 0 };
                    voxel[slice.Axis] = slice.Depth;
                    var u = (slice.Axis + 1) % 3;  // => [1, 2, 0]
                    var v = (slice.Axis + 2) % 3;  // => [2, 0, 1]
                    var offset = new NativeArray<int>(3, Allocator.Temp); // int[] offset = new int[3] { 0, 0, 0 };
                    offset[slice.Axis] = 1;
                    
                    // loop through uv
                    var n = 0;
                    var voxels = voxelBuffers[parent.Value];
                    var paletteItemEntity = blockStateEntities[parent.Value];
                    for (voxel[v] = 0; voxel[v] < slice.Dims[v]; ++voxel[v])
                    {
                        for (voxel[u] = 0; voxel[u] < slice.Dims[u]; ++voxel[u], ++n)
                        {
                            var offset3 = new int3(offset[0], offset[1], offset[2]);
                            
                            var thisChunk = isChunk ? chunks[parent.Value].id : new int3(); 
                            var aChunk = (slice.Depth == -1) ? (thisChunk - offset3) : thisChunk;
                            var aVoxel = new int3(voxel[0], voxel[1], voxel[2]);
                            var aWorldVoxel = aVoxel + aChunk * 16;
                            var aIndex = aWorldVoxel.ChunkVoxel().ToIndex(slice.Dims);
                            
                            var bWorldVoxel = aWorldVoxel + offset3;
                            var bChunk = bWorldVoxel.Chunk();
                            var bIndex = bWorldVoxel.ChunkVoxel().ToIndex(slice.Dims);

                            Entity aBlockState;
                            Entity bBlockState;
                    
                            // Edge
                            if (slice.Depth == -1)
                            {
                                if (isChunk)
                                {
                                    var aChunkEntity = chunkEntities.ContainsKey(aChunk) ? chunkEntities[aChunk] : Entity.Null; // Get chunk with a
                                    if (voxelBuffers.HasComponent(aChunkEntity) && voxelBuffers[aChunkEntity].Length == 4096 && blockStateEntities.HasComponent(aChunkEntity))
                                    {
                                        var paletteId = voxelBuffers[aChunkEntity][aIndex].Value;
                                        aBlockState = blockStateEntities[aChunkEntity][paletteId].Value;
                                    } else {
                                        aBlockState = air;
                                    }
                                } else {
                                    aBlockState = air;
                                }
                                var bPaletteId = voxels[bIndex].Value;
                                bBlockState = paletteItemEntity[bPaletteId].Value;
                            }
                    
                            // Other edge
                            else if (slice.Depth == (slice.Dims[slice.Axis] - 1))
                            {
                                if (isChunk)
                                {
                                    Entity bChunkEntity = chunkEntities.ContainsKey(bChunk) && HasComponent<LoadedDependenciesTag>(chunkEntities[bChunk]) ? chunkEntities[bChunk] : Entity.Null; // Get chunk with b
                                    if (voxelBuffers.HasComponent(bChunkEntity) && voxelBuffers[bChunkEntity].Length == 4096 && blockStateEntities.HasComponent(bChunkEntity))
                                    {
                                        var paletteId = voxelBuffers[bChunkEntity][bIndex].Value;
                                        bBlockState = blockStateEntities[bChunkEntity][paletteId].Value;
                                    } else {
                                        bBlockState = air;
                                    }
                                } else {
                                    bBlockState = air;
                                }
                                var aPaletteId = voxels[aIndex].Value;
                                aBlockState = paletteItemEntity[aPaletteId].Value;
                            }
                    
                            // Not edge
                            else
                            {
                                var aPaletteId = voxels[aIndex].Value;
                                var bPaletteId = voxels[bIndex].Value;
                                aBlockState = paletteItemEntity[aPaletteId].Value;
                                bBlockState = paletteItemEntity[bPaletteId].Value;
                            }
                            
                            if (aBlockState == Entity.Null)
                                Debug.LogError($"ablockstate:{aBlockState} v:{v} u:{u} slice.depth:{slice.Depth} isChunk:{isChunk}");
                            if (bBlockState == Entity.Null)
                                Debug.LogError($"bblockstate:{bBlockState} v:{v} u:{u} slice.depth:{slice.Depth} isChunk:{isChunk}");
                            
                            aBlockStates.Add(aBlockState);
                            bBlockStates.Add(bBlockState);
                        }
                    }
                    
                    ecb.RemoveComponent<ComputeBlockStates>(entityInQueryIndex, e);
                })
                .WithNativeDisableParallelForRestriction(voxelBuffers)
                .WithReadOnly(blockStateEntities)
                .WithReadOnly(chunks)
                .WithReadOnly(chunkEntities)
                .Schedule();
                // .ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }



    }
}