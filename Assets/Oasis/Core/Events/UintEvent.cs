using PlasticGui.WorkspaceWindow;
using UnityEngine;

namespace Oasis.Core
{
    [CreateAssetMenu(fileName = "New Uint Event", menuName = "Oasis/Events/Uint Event")]
    public class UintEvent : BaseGameEvent<uint>
    {
        // Taking int and reraising.  Inspector doesn't show uints. :/
        //public void Invoke(int i) {
        //    base.Invoke((uint)i);
        //}
    }
}