using Unity.Entities;


public struct FaceChunk: IComponentData
{
    public Entity Value;
}
public struct FaceChunkShared : ISharedComponentData
{
    public Entity Value;
}