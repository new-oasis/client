using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Settings : IComponentData
{
    public bool online;
    public FixedString32Bytes version;
    public int distance;
    public int height;
    public int defaultPlace;
    public int gravity;
    public bool lit;
}
