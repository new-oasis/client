using UnityEngine;
using Unity.Mathematics;


public static class Vector3ExtensionMethods
{

    public static int3 ToInt3(this Vector3 v)
    {
        return new int3 ((int)Mathf.Floor(v.x), (int)Mathf.Floor(v.y), (int)Mathf.Floor(v.z));
    }
    
    public static float3 ToFloat3(this Vector3 v)
    {
        return new float3 (v.x, v.y, v.z);
    }
    public static float4 ToFloat4(this Vector3 v)
    {
        return new float4 (v.x, v.y, (float)(v.z), 1f);
    }

}
