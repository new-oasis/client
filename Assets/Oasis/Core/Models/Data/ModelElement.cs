using System;
using Oasis.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ModelElement : IComponentData
{
    
    public float3 from;
    public float3 to;
    public int3 rotation;
    
    public bool lit; // denormed from blockState
    
    public Entity south;
    public Entity north;
    public Entity east;
    public Entity west;
    public Entity up;
    public Entity down;

    public float Width()
    {
        return to[0] - from[0];
    }
    
    public float Height()
    {
        return to[1] - from[1];
    }
    
    public float Depth()
    {
        return to[2] - from[2];
    }
}

