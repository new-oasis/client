using System.Threading.Tasks;
using BovineLabs.Event.Systems;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;

public partial class PlayerSystem : SystemBase
{
    
    protected override void OnUpdate()
    {
    }

    public async Task Move(int3 translation)
    {
        SetGravity(0);
        
        // Move player
        var characterEntity = GetSingletonEntity<FirstPersonCharacterComponent>();
        SetComponent(characterEntity, new Translation {Value = translation});

        if (translation.Chunk().Equals(default(int3)))
            World.GetExistingSystem<PlayerChunkChangeSystem>().triggerOnDefaultInt3 = true;

        // Wait for chunk
        var chunkSystem = World.GetOrCreateSystem<ChunkSystem>();
        await chunkSystem.WaitForChunk(translation.Chunk());

        SetGravity(GetSingleton<Settings>().gravity);
        
    }

    public void SetGravity(int gravity)
    {
        var characterEntity = GetSingletonEntity<FirstPersonCharacterComponent>();
        var characterComponent = GetSingleton<FirstPersonCharacterComponent>();
        characterComponent.Gravity = new float3(0, gravity, 0);
        EntityManager.SetComponentData(characterEntity, characterComponent);
    }

}