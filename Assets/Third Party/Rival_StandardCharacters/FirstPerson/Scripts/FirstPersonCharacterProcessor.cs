using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.Physics;

public struct FirstPersonCharacterProcessor : IKinematicCharacterProcessor
{
    public float DeltaTime;
    public CollisionWorld CollisionWorld;

    public ComponentDataFromEntity<KinematicCharacterBody> CharacterBodyFromEntity;
    public ComponentDataFromEntity<PhysicsMass> PhysicsMassFromEntity;
    public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityFromEntity;
    public ComponentDataFromEntity<TrackedTransform> TrackedTransformFromEntity;

    public NativeList<int> TmpRigidbodyIndexesProcessed;
    public NativeList<RaycastHit> TmpRaycastHits;
    public NativeList<ColliderCastHit> TmpColliderCastHits;
    public NativeList<DistanceHit> TmpDistanceHits;

    public Entity Entity;
    public float3 Translation;
    public quaternion Rotation;
    public float3 GroundingUp;
    public PhysicsCollider PhysicsCollider;
    public KinematicCharacterBody CharacterBody;
    public FirstPersonCharacterComponent FirstPersonCharacter;
    public FirstPersonCharacterInputs FirstPersonCharacterInputs;

    public DynamicBuffer<KinematicCharacterHit> CharacterHitsBuffer;
    public DynamicBuffer<KinematicCharacterDeferredImpulse> CharacterDeferredImpulsesBuffer;
    public DynamicBuffer<KinematicVelocityProjectionHit> VelocityProjectionHitsBuffer;
    public DynamicBuffer<StatefulKinematicCharacterHit> StatefulCharacterHitsBuffer;

    #region Processor Getters
    public CollisionWorld GetCollisionWorld => CollisionWorld;
    public ComponentDataFromEntity<KinematicCharacterBody> GetCharacterBodyFromEntity => CharacterBodyFromEntity;
    public ComponentDataFromEntity<PhysicsMass> GetPhysicsMassFromEntity => PhysicsMassFromEntity;
    public ComponentDataFromEntity<PhysicsVelocity> GetPhysicsVelocityFromEntity => PhysicsVelocityFromEntity;
    public ComponentDataFromEntity<TrackedTransform> GetTrackedTransformFromEntity => TrackedTransformFromEntity;
    public NativeList<int> GetTmpRigidbodyIndexesProcessed => TmpRigidbodyIndexesProcessed;
    public NativeList<RaycastHit> GetTmpRaycastHits => TmpRaycastHits;
    public NativeList<ColliderCastHit> GetTmpColliderCastHits => TmpColliderCastHits;
    public NativeList<DistanceHit> GetTmpDistanceHits => TmpDistanceHits;
    #endregion

    #region Processor Callbacks
    public bool CanCollideWithHit(in BasicHit hit)
    {
        return KinematicCharacterUtilities.DefaultMethods.CanCollideWithHit(in hit, in CharacterBodyFromEntity);
    }

    public bool IsGroundedOnHit(in BasicHit hit, int groundingEvaluationType)
    {
        return KinematicCharacterUtilities.DefaultMethods.IsGroundedOnHit(
            ref this,
            in hit,
            in CharacterBody,
            in PhysicsCollider,
            Entity,
            GroundingUp,
            groundingEvaluationType,
            FirstPersonCharacter.StepHandling,
            FirstPersonCharacter.MaxStepHeight,
            FirstPersonCharacter.ExtraStepChecksDistance);
    }

    public void OnMovementHit(
            ref KinematicCharacterHit hit,
            ref float3 remainingMovementDirection,
            ref float remainingMovementLength,
            float3 originalVelocityDirection,
            float hitDistance)
    {
        KinematicCharacterUtilities.DefaultMethods.OnMovementHit(
            ref this,
            ref hit,
            ref CharacterBody,
            ref VelocityProjectionHitsBuffer,
            ref Translation,
            ref remainingMovementDirection,
            ref remainingMovementLength,
            in PhysicsCollider,
            Entity,
            Rotation,
            GroundingUp,
            originalVelocityDirection,
            hitDistance,
            FirstPersonCharacter.StepHandling,
            FirstPersonCharacter.MaxStepHeight);
    }

    public void OverrideDynamicHitMasses(
        ref float characterMass,
        ref float otherMass,
        Entity characterEntity,
        Entity otherEntity,
        int otherRigidbodyIndex,
        bool otherEntityIsCharacter)
    {
    }

    public void ProjectVelocityOnHits(
        ref float3 velocity,
        ref bool characterIsGrounded,
        ref BasicHit characterGroundHit,
        in DynamicBuffer<KinematicVelocityProjectionHit> hits,
        float3 originalVelocityDirection)
    {
        // The last hit in the "hits" buffer is the latest hit. The rest of the hits are all hits so far in the movement iterations
        KinematicCharacterUtilities.DefaultMethods.ProjectVelocityOnHits(
            ref velocity,
            ref characterIsGrounded,
            ref characterGroundHit,
            in hits,
            originalVelocityDirection,
            GroundingUp,
            FirstPersonCharacter.ConstrainVelocityToGroundPlane);
    }
    #endregion

    public unsafe void OnUpdate()
    {
        GroundingUp = -math.normalizesafe(FirstPersonCharacter.Gravity);

        KinematicCharacterUtilities.InitializationUpdate(ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer);
        KinematicCharacterUtilities.ParentMovementUpdate(ref this, ref Translation, ref CharacterBody, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp, CharacterBody.WasGroundedBeforeCharacterUpdate); // safe to remove if not needed
        KinematicCharacterUtilities.GroundingUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, in PhysicsCollider, Entity, Rotation, GroundingUp);

        // Character velocity control is updated AFTER the ground has been detected, but BEFORE the character tries to move & collide with that velocity
        HandleCharacterControl();

        PreventGroundingFromFutureSlopeChange();

        if (CharacterBody.IsGrounded && CharacterBody.SimulateDynamicBody)
        {
            KinematicCharacterUtilities.DefaultMethods.UpdateGroundPushing(ref CollisionWorld, ref PhysicsMassFromEntity, ref CharacterDeferredImpulsesBuffer, ref CharacterBody, DeltaTime, FirstPersonCharacter.Gravity, 1f); // safe to remove if not needed
        }

        KinematicCharacterUtilities.MovementAndDecollisionsUpdate(ref this, ref Translation, ref CharacterBody, ref CharacterHitsBuffer, ref VelocityProjectionHitsBuffer, ref CharacterDeferredImpulsesBuffer, in PhysicsCollider, DeltaTime, Entity, Rotation, GroundingUp);
        KinematicCharacterUtilities.DefaultMethods.MovingPlatformDetection(ref TrackedTransformFromEntity, ref CharacterBodyFromEntity, ref CharacterBody); // safe to remove if not needed
        KinematicCharacterUtilities.ParentMomentumUpdate(ref TrackedTransformFromEntity, ref CharacterBody, in Translation, DeltaTime, GroundingUp); // safe to remove if not needed
        KinematicCharacterUtilities.ProcessStatefulCharacterHits(ref StatefulCharacterHitsBuffer, in CharacterHitsBuffer); // safe to remove if not needed
    }

    public unsafe void PreventGroundingFromFutureSlopeChange()
    {
        if (CharacterBody.IsGrounded && (FirstPersonCharacter.PreventGroundingWhenMovingTowardsNoGrounding || FirstPersonCharacter.HasMaxDownwardSlopeChangeAngle))
        {
            KinematicCharacterUtilities.DefaultMethods.DetectFutureSlopeChange(
                ref this,
                in CharacterBody.GroundHit,
                in CharacterBody,
                in PhysicsCollider,
                Entity,
                CharacterBody.RelativeVelocity,
                GroundingUp,
                0.05f, // verticalOffset
                0.05f, // downDetectionDepth
                DeltaTime, // deltaTimeIntoFuture
                0.25f, // secondaryNoGroundingCheckDistance
                FirstPersonCharacter.StepHandling,
                FirstPersonCharacter.MaxStepHeight,
                out bool isMovingTowardsNoGrounding,
                out bool foundSlopeHit,
                out float futureSlopeChangeAnglesRadians,
                out RaycastHit futureSlopeHit);
            if ((FirstPersonCharacter.PreventGroundingWhenMovingTowardsNoGrounding && isMovingTowardsNoGrounding) ||
                (FirstPersonCharacter.HasMaxDownwardSlopeChangeAngle && foundSlopeHit && math.degrees(futureSlopeChangeAnglesRadians) < -FirstPersonCharacter.MaxDownwardSlopeChangeAngle))
            {
                CharacterBody.IsGrounded = false;
            }
        }
    }

    public unsafe void HandleCharacterControl()
    {
        if (CharacterBody.IsGrounded)
        {
            // Move on ground
            float3 targetVelocity = FirstPersonCharacterInputs.MoveVector * FirstPersonCharacter.GroundMaxSpeed;
            CharacterControlUtilities.StandardGroundMove_Interpolated(ref CharacterBody.RelativeVelocity, targetVelocity, FirstPersonCharacter.GroundedMovementSharpness, DeltaTime, GroundingUp, CharacterBody.GroundHit.Normal);

            // Jump
            if (FirstPersonCharacterInputs.JumpRequested)
            {
                CharacterControlUtilities.StandardJump(ref CharacterBody, GroundingUp * FirstPersonCharacter.JumpSpeed, true, GroundingUp);
            }
        }
        else
        {
            // Move in air
            float3 airAcceleration = FirstPersonCharacterInputs.MoveVector * FirstPersonCharacter.AirAcceleration;
            CharacterControlUtilities.StandardAirMove(ref CharacterBody.RelativeVelocity, airAcceleration, FirstPersonCharacter.AirMaxSpeed, GroundingUp, DeltaTime, false);

            // Gravity
            CharacterControlUtilities.AccelerateVelocity(ref CharacterBody.RelativeVelocity, FirstPersonCharacter.Gravity, DeltaTime);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref CharacterBody.RelativeVelocity, DeltaTime, FirstPersonCharacter.AirDrag);
        }

        // Reset jump request
        FirstPersonCharacterInputs.JumpRequested = false;
    }
}
