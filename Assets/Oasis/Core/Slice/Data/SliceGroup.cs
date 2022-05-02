using Unity.Entities;
using Unity.Transforms;

namespace Oasis.Core
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ChunkGroup))]
    public class SliceGroup : ComponentSystemGroup { };
}


/*
Initialization Group
    BlocksGroup
    BlockStatesGroup
    ModelsGroup
    
    TexturesGroup
    
    
    ChunkGroup
        ChunkVisible
        Chunks
        ChunkSlices (ecb)
        ChunkDependencies
    SliceGroup
        SliceBlockStates
        SliceTextures
        SliceFaces (ecb)
        SliceLiquidLevels
        SliceLiquidFaces
Simulation Group
    SliceDestroyGroup
        SliceDestroyUnused
        
        
        
        
Frame 1
    ChunkSlices creates Slice+ComputeFaces in ecb
Frame 2
    SliceFaces create face children in ecb;  removes ComputeFaces
    TransformSystem creates children/parents in ecb
    
    SliceDestroyEmpty destroys because transformSystem using ecb
Frame 3

*/