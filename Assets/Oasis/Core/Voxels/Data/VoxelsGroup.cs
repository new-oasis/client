using Unity.Entities;
using Unity.Transforms;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class VoxelsGroup : ComponentSystemGroup { };
}
