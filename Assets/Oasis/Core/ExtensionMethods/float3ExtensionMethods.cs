using System;
using UnityEngine;
using Unity.Mathematics;
using System.Collections;


public static class float3ExtensionMethods
{

    public static int3 ToInt3(this float3 f)
    {
        int x = (int)Math.Floor(f.x);
        int y = (int)Math.Floor(f.y);
        int z = (int)Math.Floor(f.z);

        // int x = (int)Math.Round(f.x);
        // int y = (int)Math.Round(f.y);
        // int z = (int)Math.Round(f.z);
        return new int3(x,y,z);
    }


}
