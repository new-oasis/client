﻿using UnityEngine;

[CreateAssetMenu(menuName = "Oasis/Variable/String")]
public class StringVariable : ScriptableObject
{
    [SerializeField]
    private string value = "";

    public string Value
    {
        get { return value; }
        set { this.value = value; }
    }
}
