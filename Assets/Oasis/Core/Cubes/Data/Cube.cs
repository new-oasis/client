using Unity.Entities;

namespace Oasis.Core
{
    [GenerateAuthoringComponent]
    public struct Cube : IComponentData
    {
        public bool lit;
    }
}