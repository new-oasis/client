using Unity.Entities;

[GenerateAuthoringComponent]
public struct ColliderEntity : IComponentData
{
    public Entity Value;
}