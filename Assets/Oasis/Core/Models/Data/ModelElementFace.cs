using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ModelElementFace : IComponentData
{
    
    public Oasis.Core.Side side;
    
}

