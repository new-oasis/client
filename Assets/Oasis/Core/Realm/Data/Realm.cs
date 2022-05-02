using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Oasis.Core
{
    public struct Realm : ISharedComponentData
    {
        public DomainName Value;
    }
}
