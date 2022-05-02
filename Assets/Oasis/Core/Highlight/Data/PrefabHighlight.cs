using Unity.Entities;

[GenerateAuthoringComponent]
public struct PrefabHighlight : IComponentData
{
    public Entity Value;
}