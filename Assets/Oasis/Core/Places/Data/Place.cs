using Unity.Collections;
using Unity.Entities;

namespace Oasis.Core
{
    public struct Place : IComponentData
    {
        public DomainName Realm;
        public FixedString64Bytes Name;
        public Unity.Mathematics.int3 Xyz;
    }
}