using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine.Profiling;

namespace Rival
{
    public static class PhysicsUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool GetHitFaceNormal(RigidBody hitBody, ColliderKey colliderKey, out float3 faceNormal)
        {
            faceNormal = default;

            if (hitBody.Collider.Value.GetLeaf(colliderKey, out ChildCollider hitChildCollider))
            {
                ColliderType colliderType = hitChildCollider.Collider->Type;

                if (colliderType == ColliderType.Triangle || colliderType == ColliderType.Quad)
                {
                    BlobArray.Accessor<float3> verticesAccessor = ((PolygonCollider*)hitChildCollider.Collider)->Vertices;
                    float3 localFaceNormal = math.normalizesafe(math.cross(verticesAccessor[1] - verticesAccessor[0], verticesAccessor[2] - verticesAccessor[0]));
                    faceNormal = math.rotate(hitBody.WorldFromBody, localFaceNormal);

                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool DoesBodyHavePhysicsVelocityAndMass(in CollisionWorld collisionWorld, int rigidbodyIndex)
        {
            if (rigidbodyIndex < collisionWorld.NumDynamicBodies)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsBodyKinematic(in ComponentDataFromEntity<PhysicsMass> physicsMassFromEntity, Entity entity)
        {
            if (physicsMassFromEntity.HasComponent(entity) && physicsMassFromEntity[entity].InverseMass <= 0f)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsBodyDynamic(in PhysicsWorld physicsWorld, int rigidbodyIndex)
        {
            if (DoesBodyHavePhysicsVelocityAndMass(in physicsWorld.CollisionWorld, rigidbodyIndex))
            {
                if (physicsWorld.MotionVelocities[rigidbodyIndex].InverseMass > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsBodyDynamic(in ComponentDataFromEntity<PhysicsMass> physicsMassFromEntity, Entity entity)
        {
            if (physicsMassFromEntity.HasComponent(entity) && physicsMassFromEntity[entity].InverseMass > 0f)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntityDynamic(bool hasPhysicsVelocity, bool hasPhysicsMass, float invMass)
        {
            return hasPhysicsVelocity && hasPhysicsMass && invMass > 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsCollidable(in Material material)
        {
            if (material.CollisionResponse == CollisionResponsePolicy.Collide ||
                material.CollisionResponse == CollisionResponsePolicy.CollideRaiseCollisionEvents)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool SetCollisionResponse(RigidBody rigidBody, ColliderKey colliderKey, CollisionResponsePolicy collisionResponse)
        {
            if (rigidBody.Collider.Value.GetLeaf(colliderKey, out ChildCollider leafCollider))
            {
                if (leafCollider.Collider->CollisionType == CollisionType.Convex)
                {
                    ConvexCollider* colliderPtr = (ConvexCollider*)leafCollider.Collider;
                    Material material = colliderPtr->Material;
                    material.CollisionResponse = collisionResponse;
                    colliderPtr->Material = material;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool SetCollisionResponse(RigidBody rigidBody, CollisionResponsePolicy collisionResponse)
        {
            if (rigidBody.Collider.Value.CollisionType == CollisionType.Convex)
            {
                ConvexCollider* colliderPtr = (ConvexCollider*)rigidBody.Collider.GetUnsafePtr();
                Material material = colliderPtr->Material;
                material.CollisionResponse = collisionResponse;
                colliderPtr->Material = material;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetInvMassAtPoint(float3 point, float3 normal, RigidBody body, float invMass, float3 invInertia)
        {
            float3 centerOfMass = math.transform(body.WorldFromBody, body.Collider.Value.MassProperties.MassDistribution.Transform.pos);
            float3 arm = point - centerOfMass;
            float3 jacAng = math.cross(arm, normal);
            float3 armC = jacAng * invInertia;

            float objectMassInv = math.dot(armC, jacAng);
            objectMassInv += invMass;

            return objectMassInv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SolveCollisionVelocities(float3 pointVelocityA, float3 pointVelocityB, float3 hitNormalBTowardsA, float pointMassRatioAToTotal, out float3 addedVelocityOnA, out float3 addedVelocityOnB)
        {
            addedVelocityOnA = default;
            addedVelocityOnB = default;

            float massRatioBToTotal = 1f - pointMassRatioAToTotal;
            float AVelocityMagintudeOnHitNormal = math.dot(pointVelocityA, hitNormalBTowardsA);
            float BVelocityMagnitudeOnHitNormal = math.dot(pointVelocityB, hitNormalBTowardsA);

            if (BVelocityMagnitudeOnHitNormal > AVelocityMagintudeOnHitNormal)
            {
                float3 relativeImpactVelocity = hitNormalBTowardsA * (BVelocityMagnitudeOnHitNormal - AVelocityMagintudeOnHitNormal);
                addedVelocityOnA += relativeImpactVelocity * massRatioBToTotal;
                addedVelocityOnB += -relativeImpactVelocity * pointMassRatioAToTotal;
            }
        }
    }
}