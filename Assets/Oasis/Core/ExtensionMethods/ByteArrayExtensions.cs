using System;
using UnityEngine;
using Unity.Mathematics;
using System.Collections;


public static class ByteArrayExtensions
{

    public static byte AverageLevel(this byte[] buffer)
    {
        int count = 0;
        int sum = 0;
        foreach (var b in buffer)
        {
            if (b != 0)
            {
                count += 1;
                sum += b;
            }
        }

        if (count == 0)
            return 0;
        else
            return (byte)Math.Round((float)sum / count);
    }

}