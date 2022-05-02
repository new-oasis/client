using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace Oasis.Core
{
    public struct SideABlockState : IBufferElementData
    {
        public Entity Value;
    }
    public struct SideALiquidLevel : IBufferElementData
    {
        public byte Value;
    }
    public struct SideATexture : IBufferElementData
    {
        public Entity Value;
    }
    public struct SideACorners : IBufferElementData
    {
        public float4 Value;
    }


    public struct SideBBlockState : IBufferElementData
    {
        public Entity Value;
    }
    public struct SideBLiquidLevel : IBufferElementData
    {
        public byte Value;
    }
    public struct SideBTexture : IBufferElementData
    {
        public Entity Value;
    }
    public struct SideBCorners : IBufferElementData
    {
        public float4 Value;
    }
    
    
    // TODO liquid levels should be byte4 when Unity.Mathematics add it.
}