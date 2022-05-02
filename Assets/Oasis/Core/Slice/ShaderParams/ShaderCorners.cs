using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_Corners", MaterialPropertyFormat.Float4)]
public struct ShaderCorners : IComponentData {
    public float4 Value;
}