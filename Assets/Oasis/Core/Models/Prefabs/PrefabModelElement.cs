using Unity.Entities;

[GenerateAuthoringComponent]
public struct PrefabModelElement : IComponentData
{
    public Entity Value;
}