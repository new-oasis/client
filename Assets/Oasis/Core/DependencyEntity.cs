using Unity.Collections;
using Unity.Entities;

namespace Oasis.Core
{
    public struct DependencyEntity : IBufferElementData
    {
        public Entity Value;
    }

}