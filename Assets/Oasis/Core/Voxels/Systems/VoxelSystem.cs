using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Oasis.Core
{

    [UpdateInGroup(typeof(VoxelsGroup))]
    public partial class VoxelSystem : SystemBase
    {
        private EntityManager _em;
        public List<Oasis.Grpc.VoxelChange> _queue;
        private ChunkSystem _chunkSystem;
        private BlockStateSystem _blockStateSystem;


        protected override void OnCreate()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _queue = new List<Grpc.VoxelChange>();
            _chunkSystem = World.GetExistingSystem<ChunkSystem>();
            _blockStateSystem = World.GetExistingSystem<BlockStateSystem>();
            base.OnCreate();
        }

        protected override async void OnUpdate()
        {
            foreach (var change in _queue)
            {
                int3 voxel = new int3{x = change.Voxel.X, y = change.Voxel.Y, z = change.Voxel.Z};
                var chunk = _chunkSystem._entities[voxel.Chunk()];
                var voxels = _em.GetBuffer<VoxelElement>(chunk);
                var blockStates = _em.GetBuffer<BlockStateElement>(chunk);
                
                // Add BlockState to buffer if needed
                if (change.PaletteIndex+1 > blockStates.Length)
                    blockStates.Add(new BlockStateElement() {});
                
                // Set blockstate
                var blockStateEntity = _blockStateSystem.Load(change.BlockState);
                await EntityHelpers.WaitForDependencies(blockStateEntity);
                blockStates[change.PaletteIndex] = new BlockStateElement {Value = blockStateEntity};
                
                // Set voxel paletteIndex
                int voxelIndex = voxel.ChunkVoxel().ToIndex();
                voxels[voxelIndex] = new VoxelElement() {Value = Convert.ToByte(change.PaletteIndex)};

                // queue slice updates
                var slices = World.GetOrCreateSystem<SliceSystem>();
                slices.UpdateSlices(voxel);
            }
            _queue.Clear();
        }


    }
}