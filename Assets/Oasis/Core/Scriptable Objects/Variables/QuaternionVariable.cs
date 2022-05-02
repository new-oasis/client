using UnityEngine;

[CreateAssetMenu(menuName = "Oasis/Variable/Quaternion")]
public class QuaternionVariable : ScriptableObject
{
    public Quaternion Value;

    public void SetValue(Quaternion value)
    {
        Value = value;
    }

}
