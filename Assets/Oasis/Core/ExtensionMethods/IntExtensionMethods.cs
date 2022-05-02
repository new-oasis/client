using System;
using UnityEngine;
using Unity.Mathematics;
using System.Collections;


public static class IntExtensionMethods
{

    public static int3 ToInt3(this int i, int3 dims)
    {
        if (dims.Equals(default(int3)))
            dims = new int3(16);

        int3 xyz = new int3();
        xyz.z = i / (dims.y * dims.x);
        xyz.y = (i % (dims.y * dims.x)) / dims.x;
        xyz.x = i % dims.x;
        return xyz;
    }

}