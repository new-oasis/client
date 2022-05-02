using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Oasis.Core
{
    [GenerateAuthoringComponent]
    public struct PlayerRealm : IComponentData
    {
        public DomainName Value;
    }
}
