using Unity.Entities;

namespace Oasis.Core
{
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BlockStateGroup))]
    public class BlockStatesGroup : ComponentSystemGroup { };
}
