using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Oasis.Grpc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{

    [UpdateInGroup(typeof(ChunkGroup))]
    // [UpdateAfter(typeof(ChunkVisible))]
    public partial class ChunkSystem : SystemBase
    {
        public NativeHashMap<int3, Entity> _entities;
        private EntityManager _em;
        private BlockStateSystem _blockStateSystem;
        private EntityQuery query;

        protected override void OnCreate()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entities = new NativeHashMap<int3, Entity>(512, Allocator.Persistent);
            _blockStateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BlockStateSystem>();
            query = GetEntityQuery( new EntityQueryDesc {
                None = new ComponentType[] {typeof(VoxelElement), typeof(BlockStateElement), typeof(LoadingTag)},
                All = new ComponentType[] { typeof(Chunk), typeof(VisibleTag) }
            });
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            if (_entities.IsCreated)
                _entities.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            var entities = query.ToEntityArray(Allocator.Temp).ToArray();
            foreach (var e in entities)
                LoadAsync(e);
        }

        private async void LoadAsync(Entity e)
        {
            _em.RemoveComponent<LoadTag>(e);
            _em.AddComponent<LoadingTag>(e);
            Chunk chunk = GetComponent<Chunk>(e);
            var realm = _em.GetSharedComponentData<Realm>(e).Value;
            _em.SetComponentData(e, new ShaderColor(){Value = new float4(0,0,1,0)});
            
            try
            {
                var chunkRequest = new ChunkRequest()
                {
                    Realm = realm.ToGrpc(),
                    Xyz = chunk.id.ToInt3(),
                };
                var gChunk = await Client.Instance.client.GetChunkAsync(chunkRequest, Client.Instance.Metadata);
                
                var voxels = _em.AddBuffer<VoxelElement>(e);
                for (var i = 0; i < 4096; i++)
                    voxels.Add(new VoxelElement {Value = gChunk.Voxels[i]});
                
                // Load blockstates to list, then buffer;  Prevents LaodedDependencies firing on first add
                var blockStatesEntities = new List<Entity>();
                
                foreach (var gBlockState in gChunk.Palette)
                {
                    if (gBlockState.Block.Name.ToString() != "air")
                        gBlockState.Block.Version = realm.version.ToString();
                    var blockStateEntity = _blockStateSystem.Load(gBlockState);
                    blockStatesEntities.Add(blockStateEntity);
                    await EntityHelpers.WaitForDependencies(blockStateEntity);
                }
                
                var blockStates = _em.AddBuffer<BlockStateElement>(e);
                foreach (var blockStatesEntity in blockStatesEntities)
                    blockStates.Add(new BlockStateElement {Value = blockStatesEntity});
                
                
                
                if (gChunk.Palette.Count == 0 && gChunk.Palette[0].Block.Name == "air")
                    _em.AddComponent<Empty>(e);
                
                _em.RemoveComponent<LoadingTag>(e);
                _em.RemoveComponent<PhysicsCollider>(e);
                _em.AddComponent<LoadedTag>(e);
                _em.AddComponent<DisableRendering>(e);
            }
            catch (RpcException exception)
            {
                _em.SetComponentData(e, new ShaderColor(){Value = new float4(1,0,0,0)});
                Debug.LogWarning($"Chunk#Create {chunk.id} \t {exception.Message}");
            }
        }


        public Entity Create(int3 id)
        {
            Entity prefab = GetSingleton<ChunkPrefab>().Value;
            var e = EntityManager.Instantiate(prefab);
            _entities[id] = e;
            EntityManager.SetName(e, $"Chunk {id.ToStr()}");
            
            EntityManager.AddComponentData(e, new Chunk {id = id});
        
            // LTW and Translation
            EntityManager.AddComponentData(e, new LocalToWorld {Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0)});
            EntityManager.AddComponentData(e, new Translation { Value = new float3(id) * 16 });
            
            EntityManager.AddComponent<CreateSlices>(e);
            EntityManager.AddComponent<LoadTag>(e);
            return e;
        }

        
        
        
        
        public void UnloadAll()
        {
            foreach (var chunk in _entities)
            {
                EntityHelpers.DestroyWithChildren(chunk.Value);
            }
            _entities.Clear();
        }

        public async Task<Entity> WaitForChunk(int3 xyz)
        {
            _entities.TryGetValue(xyz, out var chunk);
            while (chunk.Equals(Entity.Null) || !EntityManager.HasComponent<LoadedDependenciesTag>(chunk))
            {
                await Task.Delay(20);
                _entities.TryGetValue(xyz, out chunk);
            }
            return chunk;
        }

        
        
        
        
    }
}