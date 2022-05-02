// Used for single shader to choose model|slice path
// Hope to replace with comparisson against model or slice param, but not working...so this...


using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[GenerateAuthoringComponent]
[MaterialProperty("_isModel", MaterialPropertyFormat.Float)]
public struct ShaderIsModel : IComponentData {
    public float Value;
}