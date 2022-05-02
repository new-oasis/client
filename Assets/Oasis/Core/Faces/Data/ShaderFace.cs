using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_face", MaterialPropertyFormat.Float4)]
public struct ShaderFace : IComponentData {
    public float4 Value;
}