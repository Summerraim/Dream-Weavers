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

    [Header("拥有的精灵")]
    public SpiritData[] OwnedSpirits;

    [Header("出场的精灵（0-6个）")]
    public SpiritData[] DeployedSpirits;

    [Header("配置")]
    [Range(0, 6)]
    public int MaxDeployedSpirits = 6;

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

        // 设置出场的精灵
        if (DeployedSpirits != null && OwnedSpirits != null)
        {
            foreach (var spirit in DeployedSpirits)
            {
                if (spirit != null && System.Array.IndexOf(OwnedSpirits, spirit) >= 0)
                {
                    model.DeploySpirit(spirit);
                }
            }
        }

        return model;
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
