using Unity.Entities;

[GenerateAuthoringComponent]
public struct PrefabModel : IComponentData
{
    public Entity Value;
}