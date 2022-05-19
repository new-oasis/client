using System;
using Oasis.Core;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

[UpdateInGroup(typeof(BlockStatesGroup))]
public partial class BlockStateDependencies : SystemBase
{
    EndInitializationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var textureSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TextureSystem>();

        // Cube|Liquid side textures loaded?
        Entities
            .WithNone<LoadedDependenciesTag, ModelRecord>()
            .ForEach((Entity e, int entityInQueryIndex, ref BlockState blockState) =>
            {
                // if (blockState.blockType == BlockType.cube || blockState.blockType == BlockType.liquid)
                if (!(
                        (blockState.up != Entity.Null && !HasComponent<LoadedTag>(blockState.up)) ||
                        (blockState.down != Entity.Null && !HasComponent<LoadedTag>(blockState.down)) ||
                        (blockState.north != Entity.Null && !HasComponent<LoadedTag>(blockState.north)) ||
                        (blockState.south != Entity.Null && !HasComponent<LoadedTag>(blockState.south)) ||
                        (blockState.west != Entity.Null && !HasComponent<LoadedTag>(blockState.west)) ||
                        (blockState.east != Entity.Null && !HasComponent<LoadedTag>(blockState.east))
                    ))
                    ecb.AddComponent<LoadedDependenciesTag>(entityInQueryIndex, e);
                // }).WithoutBurst().Run();
            }).Schedule();

        _ecbSystem.AddJobHandleForProducer(this.Dependency);
        
        
        
        // ModelRecord textures loaded?
        // Below can't be job because of textureSystem.entities
        Entities
            .WithNone<LoadedDependenciesTag>()
            .ForEach((Entity e, ref BlockState blockState, in ModelRecord modelRecord) =>
            {
                var hasTextures = true;

                foreach (var texture in modelRecord.Value.Textures)
                {
                    var gDomainName = new Oasis.Grpc.DomainName { Domain = "minecraft", Name = texture.Value };
                    // TODO modelRecord should ref texture entities, not texture names requiring lookup
                    
                    if (!textureSystem._entities.ContainsKey(gDomainName))
                        Debug.LogError($"BlockStateDependencies no texture entity found for {gDomainName}");
                    var textureEntity = textureSystem._entities[gDomainName];
                    hasTextures = hasTextures && HasComponent<LoadedTag>(textureEntity);
                }

                if (hasTextures)
                    EntityManager.AddComponent<LoadedDependenciesTag>(e);
            }).WithStructuralChanges().WithoutBurst().Run();

    }
}