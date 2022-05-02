using Unity.Entities;

[GenerateAuthoringComponent]
public struct PrefabSlice : IComponentData
{
    public Entity Value;
}
