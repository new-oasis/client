using Unity.Entities;

[GenerateAuthoringComponent]
public struct ChunkPrefab : IComponentData
{
    public Entity Value;
}