using UnityEngine;
using Unity.Mathematics;

namespace Oasis.Core
{
    [CreateAssetMenu(fileName = "New Int3 Event", menuName = "Oasis/Events/Int3 Event")]
    public class Int3Event : BaseGameEvent<int3>
    {
        public int3 custom;
    }
}