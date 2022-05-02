using System;
using System.Threading.Tasks;
using Oasis.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{

    [UpdateInGroup(typeof(SliceGroup))]
    public partial class SliceSystem : SystemBase
    {
        private ChunkSystem _chunkSystem;
        
        protected override void OnUpdate()
        {
        }


        protected override void OnCreate()
        {
        }


        public void UpdateSlices(int3 voxel)
        {
            // Debug.Log($"Slices#Update: Chunk: {voxel.chunk}   Voxel: {voxel.chunkVoxel}   Id: {voxel.id}  Data: {voxel.data}");
            var chunks = World.GetOrCreateSystem<ChunkSystem>();

            if (chunks._entities.TryGetValue(voxel.xyz.Chunk(), out Entity chunk))
            {
                if (HasComponent<Empty>(chunk) || HasComponent<Full>(chunk))
                    Debug.LogError("Slices#UpdateSlices TODO handle updates to chunks tagged Empty|Full");

                foreach (Side side in (Side[]) Enum.GetValues(typeof(Side)))
                    CreateForVoxelSide(chunk, voxel, side); 
            }
            else
            {
                Debug.LogError("Slices#UpdateSlices chunk not found");
            }
        }

        public void CreateForVoxelSide(Entity parent, int3 voxel, Side side)
        {
            int[] voxelArr = voxel.xyz.ChunkVoxel().ToArr();
            int voxelArrIndex = (int) side % 3;
            int depth = voxelArr[voxelArrIndex];
            if ((int) side > 2) // south, bottom, west
                depth -= 1;
            int axis = (int) side % 3;

            var e = Create(parent, depth, axis);
            // EntityManager.SetName(e, $"Slice {voxel} {side}");
        }

        public static Entity Create(Entity parent, int depth, int axis)
        {
            var dims = new int3(16);
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
               
            var slice = em.CreateEntity();
            em.SetName(slice, $"Slice {axis} {depth}");
   
            em.AddBuffer<SideABlockState>(slice);
            em.AddBuffer<SideBBlockState>(slice);
            em.AddComponent<ComputeBlockStates>(slice);
               
            em.AddBuffer<SideATexture>(slice);
            em.AddBuffer<SideBTexture>(slice);
            em.AddComponent<ComputeTextures>(slice);
               
            em.AddComponent<ComputeFaces>(slice);
            em.AddComponent<RemoveUnused>(slice);
            em.AddComponent<DupeCheck>(slice);
               
            em.AddComponentData(slice, new Slice {Depth = depth, Axis = axis, Dims = dims, Lit = true});
            em.AddComponentData(slice, new Parent {Value = parent});
            em.AddComponentData(slice, new LocalToWorld { });
            em.AddComponentData(slice, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
    
            return slice;
        }
        
        public static void CreateSlice(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity parent, int depth, int axis, bool lit, int3 dims = new int3())
        {
            var slice = ecb.CreateEntity(entityInQueryIndex);

            if (dims.Equals(default(int3)))
                dims = new int3(16);

            ecb.AddBuffer<SideABlockState>(entityInQueryIndex, slice);
            ecb.AddBuffer<SideBBlockState>(entityInQueryIndex, slice);
            ecb.AddComponent<ComputeBlockStates>(entityInQueryIndex, slice);
            
            ecb.AddBuffer<SideATexture>(entityInQueryIndex, slice);
            ecb.AddBuffer<SideBTexture>(entityInQueryIndex, slice);
            ecb.AddComponent<ComputeTextures>(entityInQueryIndex, slice);
            
            ecb.AddComponent<ComputeFaces>(entityInQueryIndex, slice);
            ecb.AddComponent<RemoveUnused>(entityInQueryIndex, slice);
            
            ecb.AddComponent(entityInQueryIndex, slice, new Slice {Depth = depth, Axis = axis, Dims = dims, Lit = lit});
            ecb.AddComponent(entityInQueryIndex, slice, new Parent {Value = parent});
            ecb.AddComponent(entityInQueryIndex, slice, new LocalToWorld { });
            ecb.AddComponent(entityInQueryIndex, slice, new LocalToParent {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)});
        }
    }

}