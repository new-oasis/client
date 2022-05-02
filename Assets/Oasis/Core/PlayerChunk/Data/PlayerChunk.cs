using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PlayerChunk : IComponentData
{
    public int3 Value;
}
