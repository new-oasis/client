using Oasis.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Oasis.Core
{
    [GenerateAuthoringComponent]
    public struct Highlight : IComponentData
    {
        public Side Side;
        public Quadrant Quadrant;
        public Facing Facing;
        public int3 Chunk;
        public int3 Voxel;
        public int3 VoxelAdjacent;
        public Entity HitEntity;
    }
}