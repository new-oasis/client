using Unity.Entities;
using Unity.Transforms;

namespace Oasis.Core
{
    // IMPORTANT;
    // Slices created in EndInitializationECB and Children created in EndSimulationECB
    // Cleanup needs both ECBs to complete
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(ChunkGroup))]
    public class SliceCleanupGroup : ComponentSystemGroup { };
}
