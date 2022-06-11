using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(BlockStatesGroup))]
    public partial class BlockStateSystem : SystemBase
    {
        private TextureSystem _textureSystem;
        public Dictionary<Grpc.BlockState, Entity> entities;
        public Entity Air;

        protected override void OnCreate()
        {
            _textureSystem = World.GetOrCreateSystem<TextureSystem>();
            entities = new Dictionary<Grpc.BlockState, Entity>();

            var gDomainName = new Grpc.DomainName() {Domain = "minecraft", Name = "air"};
            var gBlockState = new Grpc.BlockState() {Block = gDomainName};
            Air = EntityManager.CreateEntity();
            EntityManager.SetName(Air, "BlockState: air");
            EntityManager.AddComponentData(Air, new BlockState()
            {
                domainName = new DomainName(gDomainName),
                textureType = Core.TextureType.None,
                blockType = Core.BlockType.air
            });
            EntityManager.AddBuffer<StateElement>(Air);
            EntityManager.AddComponentData(Air, new LoadedTag());
            EntityManager.AddComponentData(Air, new LoadedDependenciesTag());

            entities[gBlockState] = Air;
            base.OnCreate();
        }

        public Entity Load(Oasis.Grpc.BlockState gBlockState)
        {
            if (gBlockState.Block.Name != "air" && gBlockState.Block.Version == "")
                Debug.LogError("missing version");

            if (entities.ContainsKey(gBlockState))
                return entities[gBlockState];

            Entity e = EntityManager.CreateEntity(typeof(LoadingTag));
            entities[gBlockState] = e;
            EntityManager.SetName(e, $"BlockState {gBlockState.Block.Domain}/{gBlockState.Block.Name}");
            LoadAsync(e, gBlockState);
            return e;
        }

        async void LoadAsync(Entity e, Oasis.Grpc.BlockState gBlockState)
        {
            var blockState = new BlockState()
            {
                domainName = new DomainName(gBlockState.Block),
            };

            // States
            var states = EntityManager.AddBuffer<StateElement>(e);
            foreach (var state in gBlockState.State)
                states.Add(new StateElement {Key = state.Key, Value = state.Value});

            // BlockType and TextureType
            var gModel = await Client.Instance.client.GetBlockStateAsync(gBlockState, Client.Instance.Metadata);
            blockState.blockType = SetBlockType(gModel.BlockType);
            blockState.textureType = SetTextureType(gModel.TextureType);

            if (gModel.BlockType == Grpc.BlockType.Cube)
            {
                SetTextures(ref blockState, gModel);
            }
            else if (gModel.BlockType == Grpc.BlockType.Air)
            {
                EntityManager.AddComponent<LoadedDependenciesTag>(e);
            }
            else if (gModel.BlockType == Grpc.BlockType.Liquid)
            {
                blockState.still = _textureSystem.Load(new Grpc.DomainName()
                    {Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain, Name = gModel.Textures["still"]});
                blockState.flow = _textureSystem.Load(new Grpc.DomainName()
                    {Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain, Name = gModel.Textures["flow"]});
                
                // Extract liquid level from state
                if (gBlockState.State.TryGetValue("level", out string level))
                    blockState.level = (byte)int.Parse(level);
            }
            // else if (gModel.BlockType == Grpc.BlockType.Model && gModel.Type == "block")
            // {
            //     // Debug.Log($"BlockStateSystem#LoadAsync model block {gBlockState}");
            //     SetTextures(ref blockState, gModel);
            // }
            else if (gModel.BlockType == Grpc.BlockType.Model)
            {
                // Debug.Log($"BlockStateSystem#LoadAsync Model {gBlockState}");
                EntityManager.AddSharedComponentData(e, new ModelRecord {Value = gModel});
                foreach (Grpc.Model.Types.Element element in gModel.Elements)
                {
                    foreach (KeyValuePair<string, Grpc.Model.Types.Face> kvp in element.Faces)
                    {
                        var actual = ResolveTexture(kvp.Value.Texture, gModel.Textures);
                        _textureSystem.Load(new Grpc.DomainName {Version = gBlockState.Block.Version, Domain = "minecraft", Name = actual});
                    }
                }
            }

            // Rotation
            blockState.x = gModel.X;
            blockState.y = gModel.Y;

            EntityManager.AddComponentData(e, blockState);
            EntityManager.RemoveComponent<LoadingTag>(e);
            EntityManager.AddComponent<LoadedTag>(e);
            EntityManager.SetComponentData(e, blockState);
        }

        public async Task<Entity> Create(Oasis.Grpc.BlockState gBlockState, bool lit = true)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var blockStateEntity = Load(gBlockState);
            await EntityHelpers.WaitForDependencies(blockStateEntity);

            var blockState = em.GetComponentData<BlockState>(blockStateEntity);

            Entity e = Entity.Null;
            if (blockState.blockType == Core.BlockType.model)
            {
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<PrefabModel>());
                var prefab = query.GetSingleton<PrefabModel>().Value;
                e = em.Instantiate(prefab);
                em.SetName(e, "MyModel");
                em.AddComponentData(e, new ModelInstance() {blockState = blockStateEntity});
                em.AddComponentData(e, new Translation() {Value = new float3(0, 0, 0)});

                // Rotation
                var q = Unity.Mathematics.quaternion.EulerXYZ(blockState.x * Mathf.Deg2Rad, blockState.y * Mathf.Deg2Rad, 0);
                em.AddComponentData(e, new Rotation() {Value = q});

                // ModelElementSystem takes over
            }
            else if (blockState.blockType == BlockType.liquid)
            {
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<CubePrefab>());
                var prefab = query.GetSingleton<CubePrefab>().Value;
                e = em.Instantiate(prefab);
                em.SetName(e, "MyCube");
                em.AddComponent<LoadedDependenciesTag>(e);

                // Voxel
                var voxels = em.AddBuffer<VoxelElement>(e);
                voxels.Add(new VoxelElement {Value = 0});

                // BlockStates buffer
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.AddBuffer<BlockStateElement>(e);
                ecb.AppendToBuffer(e, new BlockStateElement {Value = blockStateEntity});
                ecb.Playback(em);
                // CubeCreate system takes over..
            }
            else if (blockState.blockType == BlockType.cube)
            {
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<CubePrefab>());
                var prefab = query.GetSingleton<CubePrefab>().Value;
                e = em.Instantiate(prefab);
                em.SetName(e, "MyCube");
                em.AddComponent<LoadedDependenciesTag>(e);

                // Voxel
                var voxels = em.AddBuffer<VoxelElement>(e);
                voxels.Add(new VoxelElement {Value = 0});

                // BlockStates buffer
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.AddBuffer<BlockStateElement>(e);
                ecb.AppendToBuffer(e, new BlockStateElement {Value = blockStateEntity});
                ecb.Playback(em);
                // CubeCreate system takes over..
            }

            // TODO Wait for children?
            return e;
        }


        protected override void OnUpdate()
        {
            // throw new NotImplementedException();
        }

        string ResolveTexture(string source, MapField<string, string> textures)
        {
            if (source.StartsWith("#"))
            {
                var textureRef = source.Substring(1);
                var actual = textures[textureRef];
                return actual.Replace("block/", "");
            }

            return source;
        }

        void SetTextures(ref BlockState blockState, Grpc.Model gModel)
        {
            if (gModel.Up != null)
                blockState.up = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.Up, gModel.Textures)
                });
            if (gModel.Down != null)
                blockState.down = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.Down, gModel.Textures)
                });
            if (gModel.North != null)
                blockState.north = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.North, gModel.Textures)
                });
            if (gModel.South != null)
                blockState.south = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.South, gModel.Textures)
                });
            if (gModel.West != null)
                blockState.west = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.West, gModel.Textures)
                });
            if (gModel.East != null)
                blockState.east = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.East, gModel.Textures)
                });
        }

        private BlockType SetBlockType(Grpc.BlockType gBlockType)
        {
            switch (gBlockType)
            {
                case Grpc.BlockType.Cube:
                    return BlockType.cube;
                case Grpc.BlockType.Liquid:
                    return BlockType.liquid;
                case Grpc.BlockType.Model:
                    return BlockType.model;
                case Grpc.BlockType.Air:
                    return BlockType.air;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TextureType SetTextureType(Grpc.TextureType gTextureType)
        {
            switch (gTextureType)
            {
                case Grpc.TextureType.Opaque:
                    return TextureType.Opaque;
                case Grpc.TextureType.Transparent:
                    return TextureType.Transparent;
                case Grpc.TextureType.Alphaclip:
                    return TextureType.AlphaClip;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}