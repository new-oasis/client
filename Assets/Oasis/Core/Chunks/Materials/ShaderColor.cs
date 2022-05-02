using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_color", MaterialPropertyFormat.Float4)]
public struct ShaderColor : IComponentData {
    public float4 Value;
}