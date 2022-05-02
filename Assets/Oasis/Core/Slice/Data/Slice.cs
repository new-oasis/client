using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Slice : IComponentData
{
    public int Axis;
    public int Depth;
    public int3 Dims;
    public bool Lit;

    public override string ToString()
    {
        return $"Axis:{Axis} Depth:{Depth}";
    }

    public int Size()
    {
        var u = (Axis + 1) % 3;  // => [1, 2, 0]
        var v = (Axis + 2) % 3;  // => [2, 0, 1]
        return Dims[u] * Dims[v];
    }
};