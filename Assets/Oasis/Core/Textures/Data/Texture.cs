using Oasis.Core;
using Unity.Collections;
using Unity.Entities;

namespace Oasis.Core
{
    public struct Texture : IComponentData
    {
        public DomainName domainName; 
        public int index; // texturearray index
        public TextureType type; 
        
        
        public Texture(DomainName domainName)
        {
            this.domainName = domainName;
            this.index = -1;
            this.type = TextureType.None;
        }
    }
}