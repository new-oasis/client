using System;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.Graphs;

namespace Oasis.Core
{
    
    public struct StateElement : IBufferElementData
    {
        public FixedString32Bytes Key;
        public FixedString32Bytes Value;
    }
    
    public struct BlockState : IComponentData
    {
        public Core.DomainName domainName;

        public Core.BlockType blockType;
        public Core.TextureType textureType;
        
        // Liquid
        public bool liquid;
        public byte level; // Liquid level;  From state/block for mesher
        
        // Textures
        public Entity up;
        public Entity down;
        public Entity north;
        public Entity south;
        public Entity west;
        public Entity east;

        public int x;
        public int y;
    }
}
