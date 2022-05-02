using Unity.Collections;
using Unity.Entities;

namespace Oasis.Core
{
    public struct BlockStateElement : IBufferElementData
    {
        public Entity Value;
    }

}