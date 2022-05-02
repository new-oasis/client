using UnityEngine;

[CreateAssetMenu(menuName = "Oasis/Variable/Transform")]
public class TransformVariable : ScriptableObject
{
    [SerializeField]
    private Transform value;

    public Transform Value
    {
        get { return value; }
        set { this.value = value; }
    }
}
