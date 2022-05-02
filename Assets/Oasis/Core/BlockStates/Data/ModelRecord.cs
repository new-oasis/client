using System;
using Unity.Entities;

[System.Serializable]
public struct ModelRecord : ISharedComponentData, IEquatable<ModelRecord>
{
    public Oasis.Grpc.Model Value;
    
    
    public bool Equals(ModelRecord other)
    {
        return Value == other.Value;
    }
 
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    
}