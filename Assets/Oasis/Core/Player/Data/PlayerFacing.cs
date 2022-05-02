using Unity.Entities;

[GenerateAuthoringComponent]
public struct PlayerFacing : IComponentData
{
    public Oasis.Core.Facing Value;
}
