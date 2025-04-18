using System.Threading.Tasks;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MainCameraSystem : SystemBase
{
    public Transform CameraGameObjectTransform;

    protected override void OnUpdate()
    {
        if (CameraGameObjectTransform && HasSingleton<MainEntityCamera>())
        {
            Entity mainEntityCameraEntity = GetSingletonEntity<MainEntityCamera>();

            LocalToWorld targetLocalToWorld = GetComponent<LocalToWorld>(mainEntityCameraEntity);
            CameraGameObjectTransform.position = targetLocalToWorld.Position;
            CameraGameObjectTransform.rotation = targetLocalToWorld.Rotation;
        }
    }
}