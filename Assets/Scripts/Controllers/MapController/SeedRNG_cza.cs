using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SeedRNG_cza : MonoBehaviour
{
    private int state;

    public SeedRNG_cza(int seed)
    {
        state = seed;
    }

    // 一个简单但稳定的 hash 混合器（Xorshift）
    private int Next()
    {
        state ^= (state << 13);
        state ^= (state >> 17);
        state ^= (state << 5);
        return Math.Abs(state);
    }

    public float NextFloat()
    {
        return Next() / (float)int.MaxValue;
    }

    public int NextInt(int min, int max)
    {
        return min + (Next() % (max - min));
    }

    public T Choice<T>(T[] array)
    {
        int index = NextInt(0, array.Length);
        return array[index];
    }
}