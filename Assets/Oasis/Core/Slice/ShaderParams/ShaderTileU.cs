using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_tileX", MaterialPropertyFormat.Float)]
public struct ShaderTileU : IComponentData {
    public float Value;
}