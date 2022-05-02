using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FirstPersonPlayerSystem))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class FirstPersonCharacterRotationSystem : SystemBase
{
    public EntityQuery CharacterQuery;

    protected override void OnCreate()
    {
        CharacterQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(FirstPersonCharacterComponent),
                    typeof(FirstPersonCharacterInputs),
                }),
        });

        RequireForUpdate(CharacterQuery);
    }

    protected unsafe override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float fixedDeltaTime = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>().RateManager.Timestep;

        Entities.ForEach((
            Entity entity,
            ref CharacterInterpolation characterInterpolation,
            ref FirstPersonCharacterComponent character,
            in FirstPersonCharacterInputs characterInputs,
            in KinematicCharacterBody characterBody) =>
        {
            Rotation characterRotation = GetComponent<Rotation>(entity);
            Rotation localViewRotation = GetComponent<Rotation>(character.CharacterViewEntity);
            float3 characterUp = math.mul(characterRotation.Value, math.up());
            float3 characterRight = math.mul(characterRotation.Value, math.right());

            // Compute character & view rotations from rotation input
            FirstPersonCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                ref characterRotation.Value,
                ref localViewRotation.Value,
                ref character.ViewPitchDegrees,
                characterInputs.LookYawPitchDegrees,
                0f,
                character.MinVAngle,
                character.MaxVAngle,
                out float canceledPitchDegrees);

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well)
            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref characterRotation.Value, in characterBody, fixedDeltaTime, deltaTime);

            // Prevent rotation interpolation
            characterInterpolation.SkipNextRotationInterpolation();

            // Apply character & view rotations
            SetComponent(entity, characterRotation);
            SetComponent(character.CharacterViewEntity, localViewRotation);
        }).Schedule();
    }
}