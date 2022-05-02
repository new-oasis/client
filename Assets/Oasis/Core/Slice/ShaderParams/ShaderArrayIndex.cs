using Unity.Entities;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_index", MaterialPropertyFormat.Float)]
public struct ShaderArrayIndex : IComponentData {
    public float Value;
}