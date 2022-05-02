using Unity.Entities;
using Oasis.Core;


namespace Oasis.Core
{
    public struct LiquidFace : IComponentData 
    {

        public Entity parent;
        public Side side;
        public int depth;
        public int u;
        public int v;
        public bool lit;
        public Entity texture;
        
        // voxel xyz;   should be byte?
        public int x;
        public int y;
        public int z;
        
    }
}