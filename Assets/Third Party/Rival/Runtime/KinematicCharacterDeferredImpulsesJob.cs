using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;

namespace Rival
{
    [BurstCompile]
    public struct KinematicCharacterDeferredImpulsesJob : IJobChunk
    {
        public BufferTypeHandle<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBufferType;

        public ComponentDataFromEntity<KinematicCharacterBody> CharacterBodyFromEntity;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
        public ComponentDataFromEntity<Translation> TranslationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<Rotation> RotationFromEntity;
        [ReadOnly]
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            BufferAccessor<KinematicCharacterDeferredImpulse> chunkCharacterrDeferredImpulsesBuffers = chunk.GetBufferAccessor(CharacterDeferredImpulsesBufferType);

            for (int i = 0; i < chunk.Count; i++)
            {
                DynamicBuffer<KinematicCharacterDeferredImpulse> characterDeferredImpulsesBuffer = chunkCharacterrDeferredImpulsesBuffers[i];
                for (int deferredImpulseIndex = 0; deferredImpulseIndex < characterDeferredImpulsesBuffer.Length; deferredImpulseIndex++)
                {
                    KinematicCharacterDeferredImpulse deferredImpulse = characterDeferredImpulsesBuffer[deferredImpulseIndex];

                    // Impulse
                    bool isImpulseOnCharacter = CharacterBodyFromEntity.HasComponent(deferredImpulse.OnEntity);
                    if (isImpulseOnCharacter)
                    {
                        KinematicCharacterBody hitCharacterBody = CharacterBodyFromEntity[deferredImpulse.OnEntity];
                        hitCharacterBody.RelativeVelocity += deferredImpulse.Impulse;
                        CharacterBodyFromEntity[deferredImpulse.OnEntity] = hitCharacterBody;
                    }
                    else
                    {
                        PhysicsVelocity bodyPhysicsVelocity = PhysicsVelocityFromEntity[deferredImpulse.OnEntity];
                        PhysicsMass bodyPhysicsMass = PhysicsMassFromEntity[deferredImpulse.OnEntity];
                        Translation bodyTranslation = TranslationFromEntity[deferredImpulse.OnEntity];
                        Rotation bodyRotation = RotationFromEntity[deferredImpulse.OnEntity];

                        bodyPhysicsVelocity.ApplyImpulse(bodyPhysicsMass, bodyTranslation, bodyRotation, deferredImpulse.Impulse, deferredImpulse.AtPoint);

                        PhysicsVelocityFromEntity[deferredImpulse.OnEntity] = bodyPhysicsVelocity;
                    }

                    // Displacement
                    if (math.lengthsq(deferredImpulse.Displacement) > 0f)
                    {
                        Translation bodyTranslation = TranslationFromEntity[deferredImpulse.OnEntity];
                        bodyTranslation.Value += deferredImpulse.Displacement;
                        TranslationFromEntity[deferredImpulse.OnEntity] = bodyTranslation;
                    }
                }
            }
        }
    }
}