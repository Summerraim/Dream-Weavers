using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家数据配置（ScriptableObject，可在编辑器中创建）
/// 类似于SpiritData，用于在编辑器中配置玩家的初始状态
/// </summary>
[CreateAssetMenu(menuName = "Data/Player")]
public class PlayerData : ScriptableObject
{
    [Header("玩家基础信息")]
    public string PlayerName = "Player";

    [Header("初始道具")]
    public ItemStackConfig[] InitialItems;

    [Header("初始拥有的精灵（编辑器配置的初始值）")]
    [SerializeField]
    private SpiritData[] initialOwnedSpirits;

    [Header("初始出场的精灵（编辑器配置的初始值）")]
    [SerializeField]
    private SpiritData[] initialDeployedSpirits;

    [Header("拥有的精灵（运行时会变化）")]
    public SpiritData[] OwnedSpirits;

    [Header("出场的精灵（运行时会变化，0-6个）")]
    public SpiritData[] DeployedSpirits;

    [Header("配置")]
    [Range(0, 6)]
    public int MaxDeployedSpirits = 6;

    /// <summary>
    /// 游戏启动时重置为初始状态
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetAllPlayerData()
    {
        // 查找所有 PlayerData 资源并重置
        var allPlayerData = Resources.FindObjectsOfTypeAll<PlayerData>();
        foreach (var pd in allPlayerData)
        {
            pd.ResetToInitialState();
        }
        Debug.Log($"[PlayerData] 游戏启动，重置了 {allPlayerData.Length} 个 PlayerData");
    }

    /// <summary>
    /// 重置为初始状态（使用编辑器配置的初始值）
    /// </summary>
    public void ResetToInitialState()
    {
        // 重置拥有的精灵（只有配置了初始值才重置）
        if (initialOwnedSpirits != null && initialOwnedSpirits.Length > 0)
        {
            OwnedSpirits = (SpiritData[])initialOwnedSpirits.Clone();
        }
        // 如果 initialOwnedSpirits 为空，保持 OwnedSpirits 不变

        // 重置出场的精灵（只有配置了初始值才重置）
        if (initialDeployedSpirits != null && initialDeployedSpirits.Length > 0)
        {
            DeployedSpirits = (SpiritData[])initialDeployedSpirits.Clone();
        }
        // 如果 initialDeployedSpirits 为空，保持 DeployedSpirits 不变

        Debug.Log($"[PlayerData] {name} 已重置，拥有精灵数={OwnedSpirits?.Length ?? 0}，出场精灵数={DeployedSpirits?.Length ?? 0}");
    }

    /// <summary>
    /// 获取初始道具列表
    /// </summary>
    public List<ItemStackConfig> GetInitialItems()
    {
        var list = new List<ItemStackConfig>();
        if (InitialItems != null)
        {
            list.AddRange(InitialItems);
        }
        return list;
    }

    /// <summary>
    /// 获取拥有的精灵列表
    /// </summary>
    public List<SpiritData> GetOwnedSpirits()
    {
        var list = new List<SpiritData>();
        if (OwnedSpirits != null)
        {
            list.AddRange(OwnedSpirits);
        }
        return list;
    }

    /// <summary>
    /// 获取出场的精灵列表
    /// </summary>
    public List<SpiritData> GetDeployedSpirits()
    {
        var list = new List<SpiritData>();
        if (DeployedSpirits != null)
        {
            list.AddRange(DeployedSpirits);
        }
        return list;
    }

    /// <summary>
    /// 从此配置创建Player实例
    /// </summary>
    public Player CreatePlayer()
    {
        var model = new Player();

        // 设置出场上限
        model.SetMaxDeployedSpirits(MaxDeployedSpirits);

        // 添加初始道具
        if (InitialItems != null)
        {
            foreach (var itemConfig in InitialItems)
            {
                if (itemConfig.Item != null && itemConfig.Count > 0)
                {
                    model.AddItem(itemConfig.Item, itemConfig.Count);
                }
            }
        }

        // 添加拥有的精灵
        if (OwnedSpirits != null)
        {
            foreach (var spirit in OwnedSpirits)
            {
                if (spirit != null)
                {
                    model.AddSpirit(spirit);
                }
            }
        }

        // 如果没有配置出场精灵，则从拥有的精灵中随机抽取
        if (DeployedSpirits == null || DeployedSpirits.Length == 0)
        {
            RandomizeDeployedSpirits();
        }

        // 设置出场的精灵
        if (DeployedSpirits != null)
        {
            foreach (var spirit in DeployedSpirits)
            {
                if (spirit != null)
                {
                    model.DeploySpirit(spirit);
                }
            }
        }

        return model;
    }

    /// <summary>
    /// 从拥有的精灵中随机抽取出场精灵（最多6个）
    /// </summary>
    public void RandomizeDeployedSpirits()
    {
        if (OwnedSpirits == null || OwnedSpirits.Length == 0)
        {
            DeployedSpirits = new SpiritData[0];
            Debug.Log("[PlayerData] RandomizeDeployedSpirits: 没有拥有的精灵");
            return;
        }

        // 创建拥有精灵的临时列表用于随机抽取
        var availableSpirits = new List<SpiritData>();
        foreach (var spirit in OwnedSpirits)
        {
            if (spirit != null)
            {
                availableSpirits.Add(spirit);
            }
        }

        // 确定要抽取的数量（最多6个，不超过拥有数量）
        int deployCount = Mathf.Min(MaxDeployedSpirits, availableSpirits.Count);
        var selectedSpirits = new List<SpiritData>();

        // 随机抽取
        for (int i = 0; i < deployCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableSpirits.Count);
            selectedSpirits.Add(availableSpirits[randomIndex]);
            availableSpirits.RemoveAt(randomIndex); // 移除已选择的，避免重复
        }

        DeployedSpirits = selectedSpirits.ToArray();
        Debug.Log($"[PlayerData] RandomizeDeployedSpirits: 从 {OwnedSpirits.Length} 个精灵中随机选择了 {DeployedSpirits.Length} 个出场");
        
        // 打印选择的精灵
        for (int i = 0; i < DeployedSpirits.Length; i++)
        {
            Debug.Log($"  [{i}] {DeployedSpirits[i].DisplayName}");
        }
    }

    /// <summary>
    /// 验证数据的有效性
    /// </summary>
    private void OnValidate()
    {
        // 确保出场的精灵数量不超过上限
        if (DeployedSpirits != null && DeployedSpirits.Length > MaxDeployedSpirits)
        {
            Debug.LogWarning(
                $"[PlayerData] {name}: 出场精灵数量 ({DeployedSpirits.Length}) 超过上限 ({MaxDeployedSpirits})"
            );
        }
    }
}

/// <summary>
/// 道具配置（用于在Inspector中配置初始道具）
/// </summary>
[Serializable]
public class ItemStackConfig
{
    public ItemData Item;

    [Min(1)]
    public int Count = 1;
}
