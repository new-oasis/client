using System.Threading.Tasks;
using Oasis.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial class PlayerFacingSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<MainEntityCamera>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
            
        Entities
            .WithAll<FirstPersonCharacterComponent>()
            .ForEach((Entity e, ref Rotation rotation, ref PlayerFacing playerFacing) =>
            {
                // Debug.Log($"CharacterRotation.eulerAngles {((Quaternion) rotation.Value).eulerAngles}");
                var angles = ((Quaternion) rotation.Value).eulerAngles;
                if (angles.y > 45 && angles.y < 135)
                    playerFacing.Value = Facing.East;
                else if (angles.y > 135 && angles.y < 225)
                    playerFacing.Value = Facing.South;
                else if (angles.y > 225 && angles.y < 315)
                    playerFacing.Value = Facing.West;
                else if (angles.y > 315 && angles.y < 45)
                    playerFacing.Value = Facing.North;
            })
            .WithoutBurst()
            .Run();
    }
}
    
     
