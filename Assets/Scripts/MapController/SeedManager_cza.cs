using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedManager_cza : MonoBehaviour
{
    public static SeedManager_cza Instance;

    public int Seed { get; private set; }
    public SeedRNG_cza RNG { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化默认种子
        Seed = System.DateTime.Now.GetHashCode();
        RNG = new SeedRNG_cza(Seed);
    }

    public void SetSeed(int seed)
    {
        Seed = seed;
        RNG = new SeedRNG_cza(seed);
    }
}