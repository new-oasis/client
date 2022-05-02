using UnityEngine;
using Unity.Mathematics;


namespace Oasis.Core
{
    [CreateAssetMenu(menuName = "Oasis/Variable/Direction")]
    public class DirectionVariable : ScriptableObject
    {
        public Direction Value;

        public void SetValue(Direction value)
        {
            Value = value;
        }

        public void SetValue(DirectionVariable value)
        {
            Value = value.Value;
        }
    }
}