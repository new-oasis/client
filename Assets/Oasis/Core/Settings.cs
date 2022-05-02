using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Settings : IComponentData
{
    public int distance;
    public int height;
    public int defaultPlace;
    public int gravity;
}
