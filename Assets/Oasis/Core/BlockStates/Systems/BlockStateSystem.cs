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
using UnityEngine.Networking;
using Newtonsoft.Json;

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

            var gDomainName = new Grpc.DomainName() { Domain = "minecraft", Name = "air" };
            var gBlockState = new Grpc.BlockState() { Block = gDomainName };
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
            if (GetSingleton<Settings>().online)
                LoadAsync(e, gBlockState);
            else
                LoadLocal(e, gBlockState);

            return e;
        }


        async void LoadLocal(Entity e, Grpc.BlockState gBlockState)
        {
            // Read JSON blockstate
            var settings = GetSingleton<Settings>();
            var filePath = $"{Application.dataPath}/Resources/1.17.1/blockstates/{gBlockState.Name}.json";
            TextAsset jsonFile = Resources.Load<TextAsset>(filePath);
            if (jsonFile == null)
            {
                Debug.LogError("JSON file not found at path: " + filePath);
                return;
            }

            Oasis.Grpc.Model gModel = new Oasis.Grpc.Model;

            try
            {
                // Parse JSON
                dynamic json = JsonConvert.DeserializeObject(jsonFile.text);

                // Compute blocktype
                Oasis.Grpc.BlockType blockType = ComputeBlockType(gBlockState.Block.Name);

                // If multipart
                if (json.Multipart != null)
                    Debug.Log("Got Multipart");

                else if (blockType == Oasis.Grpc.BlockType.Air)
                    Debug.Log("Got air");

                else if (blockType == Oasis.Grpc.BlockType.Liquid)
                    Debug.Log("Got liquid");

                else if (json.Variants != null)
                    Debug.Log("Got variant");

                else
                    Debug.Log("Got simple");


                StoreBlockState(e, gBlockState, gModel);

            }
            catch (JsonReaderException ex)
            {
                Debug.LogError("Deserialization failed: " + ex.Message);
            }
        }

        async void LoadAsync(Entity e, Oasis.Grpc.BlockState gBlockState)
        {
            // BlockType and TextureType
            var gModel = await Client.Instance.client.GetBlockStateAsync(gBlockState, Client.Instance.Metadata);
            StoreBlockState(e, gBlockState, gModel);
        }

        void StoreBlockState(Entity e, Oasis.Grpc.BlockState gBlockState, Oasis.Grpc.Model gModel)
        {
            var blockState = new BlockState()
            {
                domainName = new DomainName(gBlockState.Block),
            };

            // States
            var states = EntityManager.AddBuffer<StateElement>(e);
            foreach (var state in gBlockState.State)
                states.Add(new StateElement { Key = state.Key, Value = state.Value });

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
                { Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain, Name = gModel.Textures["still"] });
                blockState.flow = _textureSystem.Load(new Grpc.DomainName()
                { Version = gModel.DomainName.Version, Domain = gModel.DomainName.Domain, Name = gModel.Textures["flow"] });

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
                EntityManager.AddSharedComponentData(e, new ModelRecord { Value = gModel });
                foreach (Grpc.Model.Types.Element element in gModel.Elements)
                {
                    foreach (KeyValuePair<string, Grpc.Model.Types.Face> kvp in element.Faces)
                    {
                        var actual = ResolveTexture(kvp.Value.Texture, gModel.Textures);
                        _textureSystem.Load(new Grpc.DomainName { Version = gBlockState.Block.Version, Domain = "minecraft", Name = actual });
                    }
                }

                // Get waterlogged value
                if (gBlockState.State.TryGetValue("waterlogged", out string waterlogged))
                    blockState.waterlogged = bool.Parse(gBlockState.State["waterlogged"]);

                // Convert facing to rotation
                if (gBlockState.State.TryGetValue("facing", out string facing))
                {
                    if (facing == "north")
                        gModel.Y += 180;
                    else if (facing == "south")
                        gModel.Y += 180;
                    else if (facing == "east")
                        gModel.Y += 180;
                    else if (facing == "west")
                        gModel.Y += 180;
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
                em.AddComponentData(e, new ModelInstance() { blockState = blockStateEntity });
                em.AddComponentData(e, new Translation() { Value = new float3(0, 0, 0) });

                // Rotation
                var q = Unity.Mathematics.quaternion.EulerXYZ(blockState.x * Mathf.Deg2Rad, blockState.y * Mathf.Deg2Rad, 0);
                em.AddComponentData(e, new Rotation() { Value = q });

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
                voxels.Add(new VoxelElement { Value = 0 });

                // BlockStates buffer
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.AddBuffer<BlockStateElement>(e);
                ecb.AppendToBuffer(e, new BlockStateElement { Value = blockStateEntity });
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
                voxels.Add(new VoxelElement { Value = 0 });

                // BlockStates buffer
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.AddBuffer<BlockStateElement>(e);
                ecb.AppendToBuffer(e, new BlockStateElement { Value = blockStateEntity });
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
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.Up, gModel.Textures)
                });
            if (gModel.Down != null)
                blockState.down = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.Down, gModel.Textures)
                });
            if (gModel.North != null)
                blockState.north = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.North, gModel.Textures)
                });
            if (gModel.South != null)
                blockState.south = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.South, gModel.Textures)
                });
            if (gModel.West != null)
                blockState.west = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
                    Name = ResolveTexture(gModel.West, gModel.Textures)
                });
            if (gModel.East != null)
                blockState.east = _textureSystem.Load(new Grpc.DomainName()
                {
                    Version = gModel.DomainName.Version,
                    Domain = gModel.DomainName.Domain,
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

        Oasis.Grpc.BlockType ComputeBlockType(string name)
        {
            List<string> cubes = new List<string>{
        // TODO update below to consider domain
		"acacia_leaves",
        "acacia_log",
        "acacia_planks",
        "acacia_pressure_plate",
        "acacia_wood",
        "amethyst_block",
        "amethyst_cluster",
        "ancient_debris",
        "andesite",
        "anvil",
        "attached_melon_stem",
        "attached_pumpkin_stem",
        "azalea",
        "azalea_leaves",
        "bamboo",
        "bamboo_small_leaves",
        "barrel",
        "barrier",
        "basalt",
        "beacon",
        "bedrock",
        "beehive",
        "bee_nest",
        "beetroots",
        "bell",
        "big_dripleaf",
        "big_dripleaf_stem",
        "birch_leaves",
        "birch_log",
        "birch_planks",
        "birch_pressure_plate",
        "birch_wood",
        "black_candle",
        "black_candle_cake",
        "black_concrete",
        "black_concrete_powder",
        "black_glazed_terracotta",
        "black_shulker_box",
        "black_stained_glass",
        "blackstone",
        "black_terracotta",
        "black_wool",
        "blast_furnace",
        "blue_candle",
        "blue_candle_cake",
        "blue_concrete",
        "blue_concrete_powder",
        "blue_glazed_terracotta",
        "blue_ice",
        "blue_shulker_box",
        "blue_stained_glass",
        "blue_terracotta",
        "blue_wool",
        "bone_block",
        "bookshelf",
        "brain_coral",
        "brain_coral_block",
        "brewing_stand",
        "bricks",
        "brown_candle",
        "brown_candle_cake",
        "brown_concrete",
        "brown_concrete_powder",
        "brown_glazed_terracotta",
        "brown_mushroom",
        "brown_shulker_box",
        "brown_stained_glass",
        "brown_terracotta",
        "brown_wool",
        "bubble_coral",
        "bubble_coral_block",
        "budding_amethyst",
        "cake",
        "calcite",
        "campfire",
        "candle",
        "candle_cake",
        "carrots",
        "cartography_table",
        "carved_pumpkin",
        "cauldron",
        "cave_vines",
        "cave_vines_plant",
        "chain",
        "chain_command_block",
        "chest",
        "chipped_anvil",
        "chiseled_deepslate",
        "chiseled_nether_bricks",
        "chiseled_polished_blackstone",
        "chiseled_quartz_block",
        "chiseled_red_sandstone",
        "chiseled_sandstone",
        "chiseled_stone_bricks",
        "chorus_plant",
        "clay",
        "coal_block",
        "coal_ore",
        "coarse_dirt",
        "cobbled_deepslate",
        "cobblestone",
        "cobweb",
        "cocoa",
        "command_block",
        "comparator",
        "composter",
        "conduit",
        "copper_block",
        "copper_ore",
        "cracked_deepslate_bricks",
        "cracked_deepslate_tiles",
        "cracked_nether_bricks",
        "cracked_polished_blackstone_bricks",
        "cracked_stone_bricks",
        "crafting_table",
        "creeper_head",
        "crimson_fungus",
        "crimson_hyphae",
        "crimson_nylium",
        "crimson_planks",
        "crimson_pressure_plate",
        "crimson_roots",
        "crimson_stem",
        "crying_obsidian",
        "cut_copper",
        "cut_red_sandstone",
        "cut_sandstone",
        "cyan_candle",
        "cyan_candle_cake",
        "cyan_concrete",
        "cyan_concrete_powder",
        "cyan_glazed_terracotta",
        "cyan_shulker_box",
        "cyan_stained_glass",
        "cyan_terracotta",
        "cyan_wool",
        "damaged_anvil",
        "dark_oak_leaves",
        "dark_oak_log",
        "dark_oak_planks",
        "dark_oak_pressure_plate",
        "dark_oak_wood",
        "dark_prismarine",
        "daylight_detector",
        "dead_brain_coral",
        "dead_brain_coral_block",
        "dead_bubble_coral",
        "dead_bubble_coral_block",
        "dead_fire_coral",
        "dead_fire_coral_block",
        "dead_horn_coral",
        "dead_horn_coral_block",
        "dead_tube_coral",
        "dead_tube_coral_block",
        "deepslate",
        "deepslate_bricks",
        "deepslate_coal_ore",
        "deepslate_copper_ore",
        "deepslate_diamond_ore",
        "deepslate_emerald_ore",
        "deepslate_gold_ore",
        "deepslate_iron_ore",
        "deepslate_lapis_ore",
        "deepslate_redstone_ore",
        "deepslate_tiles",
        "diamond_block",
        "diamond_ore",
        "diorite",
        "dirt",
        "dirt_path",
        "dispenser",
        "dragon_egg",
        "dragon_head",
        "dried_kelp_block",
        "dripstone_block",
        "dropper",
        "emerald_block",
        "emerald_ore",
        "enchanting_table",
        "ender_chest",
        "end_portal",
        "end_portal_frame",
        "end_rod",
        "end_stone",
        "end_stone_bricks",
        "exposed_copper",
        "exposed_cut_copper",
        "farmland",
        "fern",
        "fire",
        "fire_coral",
        "fire_coral_block",
        "fletching_table",
        "frosted_ice",
        "furnace",
        "gilded_blackstone",
        "glass",
        "glow_item_frame",
        "glow_lichen",
        "glowstone",
        "gold_block",
        "gold_ore",
        "granite",
        "grass_block",
        "gravel",
        "gray_candle",
        "gray_candle_cake",
        "gray_concrete",
        "gray_concrete_powder",
        "gray_glazed_terracotta",
        "gray_shulker_box",
        "gray_stained_glass",
        "gray_terracotta",
        "gray_wool",
        "green_candle",
        "green_candle_cake",
        "green_concrete",
        "green_concrete_powder",
        "green_glazed_terracotta",
        "green_shulker_box",
        "green_stained_glass",
        "green_terracotta",
        "green_wool",
        "grindstone",
        "hanging_roots",
        "hay_block",
        "heavy_weighted_pressure_plate",
        "honey_block",
        "honeycomb_block",
        "hopper",
        "horn_coral",
        "horn_coral_block",
        "ice",
        "infested_chiseled_stone_bricks",
        "infested_cobblestone",
        "infested_cracked_stone_bricks",
        "infested_deepslate",
        "infested_mossy_stone_bricks",
        "infested_stone",
        "infested_stone_bricks",
        "iron_bars",
        "iron_block",
        "iron_ore",
        "item_frame",
        "jack_o_lantern",
        "jigsaw",
        "jukebox",
        "jungle_leaves",
        "jungle_log",
        "jungle_planks",
        "jungle_pressure_plate",
        "jungle_wood",
        "kelp",
        "kelp_plant",
        "lantern",
        "lapis_block",
        "lapis_ore",
        "large_amethyst_bud",
        "large_fern",
        "lava_cauldron",
        "lectern",
        "lever",
        "light",
        "light_blue_candle",
        "light_blue_candle_cake",
        "light_blue_concrete",
        "light_blue_concrete_powder",
        "light_blue_glazed_terracotta",
        "light_blue_shulker_box",
        "light_blue_stained_glass",
        "light_blue_terracotta",
        "light_blue_wool",
        "light_gray_candle",
        "light_gray_candle_cake",
        "light_gray_concrete",
        "light_gray_concrete_powder",
        "light_gray_glazed_terracotta",
        "light_gray_shulker_box",
        "light_gray_stained_glass",
        "light_gray_terracotta",
        "light_gray_wool",
        "lightning_rod",
        "light_weighted_pressure_plate",
        "lilac",
        "lime_candle",
        "lime_candle_cake",
        "lime_concrete",
        "lime_concrete_powder",
        "lime_glazed_terracotta",
        "lime_shulker_box",
        "lime_stained_glass",
        "lime_terracotta",
        "lime_wool",
        "lodestone",
        "loom",
        "magenta_candle",
        "magenta_candle_cake",
        "magenta_concrete",
        "magenta_concrete_powder",
        "magenta_glazed_terracotta",
        "magenta_shulker_box",
        "magenta_stained_glass",
        "magenta_terracotta",
        "magenta_wool",
        "magma_block",
        "medium_amethyst_bud",
        "melon",
        "melon_stem",
        "moss_block",
        "mossy_cobblestone",
        "mossy_stone_bricks",
        "moving_piston",
        "mushroom_stem",
        "mycelium",
        "nether_bricks",
        "nether_gold_ore",
        "netherite_block",
        "nether_portal",
        "nether_quartz_ore",
        "netherrack",
        "nether_sprouts",
        "nether_wart",
        "nether_wart_block",
        "note_block",
        "oak_leaves",
        "oak_log",
        "oak_planks",
        "oak_pressure_plate",
        "oak_wood",
        "observer",
        "obsidian",
        "orange_candle",
        "orange_candle_cake",
        "orange_concrete",
        "orange_concrete_powder",
        "orange_glazed_terracotta",
        "orange_shulker_box",
        "orange_stained_glass",
        "orange_terracotta",
        "orange_wool",
        "oxidized_copper",
        "oxidized_cut_copper",
        "packed_ice",
        "peony",
        "pink_candle",
        "pink_candle_cake",
        "pink_concrete",
        "pink_concrete_powder",
        "pink_glazed_terracotta",
        "pink_shulker_box",
        "pink_stained_glass",
        "pink_terracotta",
        "pink_wool",
        "piston",
        "piston_head",
        "player_head",
        "podzol",
        "pointed_dripstone",
        "polished_andesite",
        "polished_basalt",
        "polished_blackstone",
        "polished_blackstone_bricks",
        "polished_blackstone_pressure_plate",
        "polished_deepslate",
        "polished_diorite",
        "polished_granite",
        "potatoes",
        "powder_snow",
        "powder_snow_cauldron",
        "prismarine",
        "prismarine_bricks",
        "pumpkin",
        "pumpkin_stem",
        "purple_candle",
        "purple_candle_cake",
        "purple_concrete",
        "purple_concrete_powder",
        "purple_glazed_terracotta",
        "purple_shulker_box",
        "purple_stained_glass",
        "purple_terracotta",
        "purple_wool",
        "purpur_block",
        "purpur_pillar",
        "quartz_block",
        "quartz_bricks",
        "quartz_pillar",
        "raw_copper_block",
        "raw_gold_block",
        "raw_iron_block",
        "red_candle",
        "red_candle_cake",
        "red_concrete",
        "red_concrete_powder",
        "red_glazed_terracotta",
        "red_mushroom",
        "red_nether_bricks",
        "red_sand",
        "red_sandstone",
        "red_shulker_box",
        "red_stained_glass",
        "redstone_block",
        "redstone_lamp",
        "redstone_ore",
        "redstone_torch",
        "redstone_wire",
        "red_terracotta",
        "red_wool",
        "repeater",
        "repeating_command_block",
        "respawn_anchor",
        "rooted_dirt",
        "sand",
        "sandstone",
        "scaffolding",
        "sculk_sensor",
        "sea_lantern",
        "shroomlight",
        "shulker_box",
        "skeleton_skull",
        "slime_block",
        "small_amethyst_bud",
        "small_dripleaf",
        "smithing_table",
        "smoker",
        "smooth_basalt",
        "smooth_quartz",
        "smooth_red_sandstone",
        "smooth_sandstone",
        "smooth_stone",
        "snow",
        "snow_block",
        "soul_stone",
        "spawner",
        "sponge",
        "spruce_leaves",
        "spruce_log",
        "spruce_planks",
        "spruce_wood",
        "stone",
        "stone_bricks",
        "stonecutter",
        "stripped_acacia_log",
        "stripped_acacia_wood",
        "stripped_birch_log",
        "stripped_birch_wood",
        "stripped_crimson_hyphae",
        "stripped_dark_oak_log",
        "stripped_dark_oak_wood",
        "stripped_jungle_log",
        "stripped_jungle_wood",
        "stripped_oak_log",
        "stripped_oak_wood",
        "stripped_spruce_log",
        "stripped_spruce_wood",
        "stripped_warped_hyphae",
        "stripped_warped_stem",
        "structure_block",
        "structure_void",
        "target",
        "terracotta",
        "tinted_glass",
        "tnt",
        "tube_coral_block",
        "tuff",
        "wet_sponge",
        "white_concrete",
        "white_stained_glass",
        "white_terracotta",
        "white_wool",
        "yellow_concrete",
        "yellow_stained_glass",
        "yellow_terracotta",
        "yellow_wool",
    };
            List<string> models = new List<string>{
                "acacia_button",
		"acacia_door",
		"acacia_fence",
		"acacia_fence_gate",
		"acacia_sapling",
		"acacia_sign",
		"acacia_slab",
		"acacia_stairs",
		"acacia_trapdoor",
		"acacia_wall_sign",
		"activator_rail",
		"allium",
		"andesite_slab",
		"andesite_stairs",
		"andesite_wall",
		"azure_bluet",
		"bamboo_sapling",
		"birch_button",
		"birch_door",
		"birch_fence",
		"birch_fence_gate",
		"birch_sapling",
		"birch_sign",
		"birch_slab",
		"birch_stairs",
		"birch_trapdoor",
		"birch_wall_sign",
		"black_banner",
		"black_bed",
		"black_carpet",
		"black_stained_glass_pane",
		"black_wall_banner",
		"blackstone_slab",
		"blackstone_stairs",
		"blackstone_wall",
		"blue_banner",
		"blue_bed",
		"blue_carpet",
		"blue_orchid",
		"blue_stained_glass_pane",
		"blue_wall_banner",
		"brain_coral_fan",
		"brain_coral_wall_fan",
		"brick_slab",
		"brick_stairs",
		"brick_wall",
		"brown_banner",
		"brown_bed",
		"brown_carpet",
		"brown_mushroom_block",
		"brown_stained_glass_pane",
		"brown_wall_banner",
		"bubble_coral_fan",
		"bubble_coral_wall_fan",
		"cactus",
		"chorus_flower",
		"cobbled_deepslate_slab",
		"cobbled_deepslate_stairs",
		"cobbled_deepslate_wall",
		"cobblestone_slab",
		"cobblestone_stairs",
		"cobblestone_wall",
		"cornflower",
		"creeper_wall_head",
		"crimson_button",
		"crimson_door",
		"crimson_fence",
		"crimson_fence_gate",
		"crimson_sign",
		"crimson_slab",
		"crimson_stairs",
		"crimson_trapdoor",
		"crimson_wall_sign",
		"cut_copper_slab",
		"cut_copper_stairs",
		"cut_red_sandstone_slab",
		"cut_sandstone_slab",
		"cyan_banner",
		"cyan_bed",
		"cyan_carpet",
		"cyan_stained_glass_pane",
		"cyan_wall_banner",
		"dandelion",
		"dark_oak_button",
		"dark_oak_door",
		"dark_oak_fence",
		"dark_oak_fence_gate",
		"dark_oak_sapling",
		"dark_oak_sign",
		"dark_oak_slab",
		"dark_oak_stairs",
		"dark_oak_trapdoor",
		"dark_oak_wall_sign",
		"dark_prismarine_slab",
		"dark_prismarine_stairs",
		"dead_brain_coral_fan",
		"dead_brain_coral_wall_fan",
		"dead_bubble_coral_fan",
		"dead_bubble_coral_wall_fan",
		"dead_bush",
		"dead_fire_coral_fan",
		"dead_fire_coral_wall_fan",
		"dead_horn_coral_fan",
		"dead_horn_coral_wall_fan",
		"dead_tube_coral_fan",
		"dead_tube_coral_wall_fan",
		"deepslate_brick_slab",
		"deepslate_brick_stairs",
		"deepslate_brick_wall",
		"deepslate_tile_slab",
		"deepslate_tile_stairs",
		"deepslate_tile_wall",
		"detector_rail",
		"diorite_slab",
		"diorite_stairs",
		"diorite_wall",
		"dragon_wall_head",
		"end_gateway",
		"end_stone_brick_slab",
		"end_stone_brick_stairs",
		"end_stone_brick_wall",
		"exposed_cut_copper_slab",
		"exposed_cut_copper_stairs",
		"fire_coral_fan",
		"fire_coral_wall_fan",
		"flower_pot",
		"flowering_azalea",
		"flowering_azalea_leaves",
		"glass_pane",
		"granite_slab",
		"granite_stairs",
		"granite_wall",
		"grass",
		"gray_banner",
		"gray_bed",
		"gray_carpet",
		"gray_stained_glass_pane",
		"gray_wall_banner",
		"green_banner",
		"green_bed",
		"green_carpet",
		"green_stained_glass_pane",
		"green_wall_banner",
		"horn_coral_fan",
		"horn_coral_wall_fan",
		"iron_door",
		"iron_trapdoor",
		"jungle_button",
		"jungle_door",
		"jungle_fence",
		"jungle_fence_gate",
		"jungle_sapling",
		"jungle_sign",
		"jungle_slab",
		"jungle_stairs",
		"jungle_trapdoor",
		"jungle_wall_sign",
		"ladder",
		"light_blue_banner",
		"light_blue_bed",
		"light_blue_carpet",
		"light_blue_stained_glass_pane",
		"light_blue_wall_banner",
		"light_gray_banner",
		"light_gray_bed",
		"light_gray_carpet",
		"light_gray_stained_glass_pane",
		"light_gray_wall_banner",
		"lily_of_the_valley",
		"lily_pad",
		"lime_banner",
		"lime_bed",
		"lime_carpet",
		"lime_stained_glass_pane",
		"lime_wall_banner",
		"magenta_banner",
		"magenta_bed",
		"magenta_carpet",
		"magenta_stained_glass_pane",
		"magenta_wall_banner",
		"moss_carpet",
		"mossy_cobblestone_slab",
		"mossy_cobblestone_stairs",
		"mossy_cobblestone_wall",
		"mossy_stone_brick_slab",
		"mossy_stone_brick_stairs",
		"mossy_stone_brick_wall",
		"multipart",
		"nether_brick_fence",
		"nether_brick_slab",
		"nether_brick_stairs",
		"nether_brick_wall",
		"oak_button",
		"oak_door",
		"oak_fence",
		"oak_fence_gate",
		"oak_sapling",
		"oak_sign",
		"oak_slab",
		"oak_stairs",
		"oak_trapdoor",
		"oak_wall_sign",
		"orange_banner",
		"orange_bed",
		"orange_carpet",
		"orange_stained_glass_pane",
		"orange_tulip",
		"orange_wall_banner",
		"oxeye_daisy",
		"oxidized_cut_copper_slab",
		"oxidized_cut_copper_stairs",
		"petrified_oak_slab",
		"pink_banner",
		"pink_bed",
		"pink_carpet",
		"pink_stained_glass_pane",
		"pink_tulip",
		"pink_wall_banner",
		"player_wall_head",
		"polished_andesite_slab",
		"polished_andesite_stairs",
		"polished_blackstone_brick_slab",
		"polished_blackstone_brick_stairs",
		"polished_blackstone_brick_wall",
		"polished_blackstone_button",
		"polished_blackstone_slab",
		"polished_blackstone_stairs",
		"polished_blackstone_wall",
		"polished_deepslate_slab",
		"polished_deepslate_stairs",
		"polished_deepslate_wall",
		"polished_diorite_slab",
		"polished_diorite_stairs",
		"polished_granite_slab",
		"polished_granite_stairs",
		"poppy",
		"potted_acacia_sapling",
		"potted_allium",
		"potted_azalea_bush",
		"potted_azure_bluet",
		"potted_bamboo",
		"potted_birch_sapling",
		"potted_blue_orchid",
		"potted_brown_mushroom",
		"potted_cactus",
		"potted_cornflower",
		"potted_crimson_fungus",
		"potted_crimson_roots",
		"potted_dandelion",
		"potted_dark_oak_sapling",
		"potted_dead_bush",
		"potted_fern",
		"potted_flowering_azalea_bush",
		"potted_jungle_sapling",
		"potted_lily_of_the_valley",
		"potted_oak_sapling",
		"potted_orange_tulip",
		"potted_oxeye_daisy",
		"potted_pink_tulip",
		"potted_poppy",
		"potted_red_mushroom",
		"potted_red_tulip",
		"potted_spruce_sapling",
		"potted_warped_fungus",
		"potted_warped_roots",
		"potted_white_tulip",
		"potted_wither_rose",
		"powered_rail",
		"prismarine_brick_slab",
		"prismarine_brick_stairs",
		"prismarine_slab",
		"prismarine_stairs",
		"prismarine_wall",
		"purple_banner",
		"purple_bed",
		"purple_carpet",
		"purple_stained_glass_pane",
		"purple_wall_banner",
		"purpur_slab",
		"purpur_stairs",
		"quartz_slab",
		"quartz_stairs",
		"rail",
		"red_banner",
		"red_bed",
		"red_carpet",
		"red_mushroom_block",
		"red_nether_brick_slab",
		"red_nether_brick_stairs",
		"red_nether_brick_wall",
		"red_sandstone_slab",
		"red_sandstone_stairs",
		"red_sandstone_wall",
		"red_stained_glass_pane",
		"red_tulip",
		"red_wall_banner",
		"redstone_wall_torch",
		"rose_bush",
		"sandstone_slab",
		"sandstone_stairs",
		"sandstone_wall",
		"sea_pickle",
		"seagrass",
		"skeleton_wall_skull",
		"smooth_quartz_slab",
		"smooth_quartz_stairs",
		"smooth_red_sandstone_slab",
		"smooth_red_sandstone_stairs",
		"smooth_sandstone_slab",
		"smooth_sandstone_stairs",
		"smooth_stone_slab",
		"spruce_button",
		"spruce_fence",
		"spruce_fence_gate",
		"spruce_fence_post",
		"spruce_pressure_plate",
		"spruce_sapling",
		"spruce_sign",
		"spruce_slab",
		"spruce_stairs",
		"spruce_trapdoor",
		"spruce_wall_sign",
		"stone_brick_slab",
		"stone_brick_stairs",
		"stone_brick_wall",
		"stone_button",
		"stone_pressure_plate",
		"stone_slab",
		"stone_stairs",
		"sunflower",
		"sweet_berry_bush",
		"tall_grass",
		"tall_seagrass",
		"template_single_face",
		"torch",
		"tripwire",
		"tube_coral_fan",
		"tube_coral_wall_fan",
		"vine",
		"wall_torch",
		"warped_button",
		"warped_sign",
		"warped_slab",
		"warped_stairs",
		"warped_wall_sign",
		"waxed_cut_copper_slab",
		"waxed_cut_copper_stairs",
		"waxed_exposed_cut_copper_slab",
		"waxed_exposed_cut_copper_stairs",
		"waxed_oxidized_cut_copper_slab",
		"waxed_oxidized_cut_copper_stairs",
		"waxed_weathered_cut_copper_slab",
		"waxed_weathered_cut_copper_stairs",
		"weathered_cut_copper_slab",
		"weathered_cut_copper_stairs",
		"white_banner",
		"white_bed",
		"white_stained_glass_pane",
		"white_tulip",
		"white_wall_banner",
		"wither_rose",
		"yellow_banner",
		"yellow_bed",
		"yellow_stained_glass_pane",
		"yellow_wall_banner",
	}
            List<string> liquids = new List<string>{
                "lava",
		"water",
		"bubble_column",
	}
            List<string> airs = new List<string>{
                "air",
		"cave_air",
	}
            if (cubes.Contains(name))
                return Oasis.Grpc.BlockType.Cube;
            else if (models.Contains(name))
                return Oasis.Grpc.BlockType.Model;
            else if (liquids.Contains(name))
                return Oasis.Grpc.BlockType.Liquid;
            else if (airs.Contains(name))
                return Oasis.Grpc.BlockType.Air;
            else
                return Oasis.Grpc.BlockType.Noblocktype;
        }
    }

}