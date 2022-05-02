using Unity.Entities;
using Unity.Mathematics;

namespace Oasis.Core
{
    [GenerateAuthoringComponent]
    public struct Chunk : IComponentData
    {
        public int3 id;
    }
}