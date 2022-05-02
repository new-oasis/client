using UnityEngine;
using Unity.Mathematics;

[CreateAssetMenu(menuName = "Oasis/Variable/Int3")]
public class Int3Variable : ScriptableObject
{
#if UNITY_EDITOR
    [Multiline]
    public string DeveloperDescription = "";
#endif
    public int3 Value;

    public void SetValue(int3 value)
    {
        Value = value;
    }

    public void SetValue(Int3Variable value)
    {
        Value = value.Value;
    }

}
