using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_tileY", MaterialPropertyFormat.Float)]
public struct ShaderTileV : IComponentData {
    public float Value;
}