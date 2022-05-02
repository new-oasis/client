using Oasis.Core;
using Unity.Entities;
using Unity.Collections;

namespace Oasis.Core
{
    
    public struct ModelInstance : IComponentData
    {
        public Entity blockState;
        public bool lit;
    }
    
}