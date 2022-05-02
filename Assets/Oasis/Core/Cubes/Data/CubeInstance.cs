using Oasis.Core;
using Unity.Entities;
using Unity.Collections;

namespace Oasis.Core
{
    
    [GenerateAuthoringComponent]
    public struct CubeInstance : IComponentData
    {
        public bool lit;
    }
    
}