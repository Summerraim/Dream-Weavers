using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Player
{
    // —— 玩家拥有的道具 ——
    [SerializeField]
    private List<ItemStack> ownedItems = new List<ItemStack>();

    // —— 玩家拥有的所有精灵 ——
    [SerializeField]
    private List<SpiritData> ownedSpirits = new List<SpiritData>();

    // —— 出场的精灵（0-6个）——
    [SerializeField]
    private List<SpiritData> deployedSpirits = new List<SpiritData>();

    // —— 出场精灵数量上限 ——
    [SerializeField]
    private int maxDeployedSpirits = 6;

    // —— 事件：当数据变化时通知UI ——
    public event Action OnDataChanged;

    public Player()
    {
        ownedItems = new List<ItemStack>();
        ownedSpirits = new List<SpiritData>();
        deployedSpirits = new List<SpiritData>();
    }

    #region Item Management

    /// <summary>
    /// 添加道具到背包
    /// </summary>
    public bool AddItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return false;

        // 查找是否已存在该道具
        var existingStack = ownedItems.FirstOrDefault(stack => stack.Item == item);
        if (existingStack != null)
        {
            existingStack.Count += count;
        }
        else
        {
            ownedItems.Add(new ItemStack(item, count));
        }

        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 移除道具
    /// </summary>
    public bool RemoveItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return false;

        var existingStack = ownedItems.FirstOrDefault(stack => stack.Item == item);
        if (existingStack == null || existingStack.Count < count)
            return false;

        existingStack.Count -= count;
        if (existingStack.Count <= 0)
        {
            ownedItems.Remove(existingStack);
        }

        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取道具数量
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        var stack = ownedItems.FirstOrDefault(s => s.Item == item);
        return stack?.Count ?? 0;
    }

    /// <summary>
    /// 获取所有道具
    /// </summary>
    public List<ItemStack> GetAllItems()
    {
        return new List<ItemStack>(ownedItems);
    }

    #endregion

    #region Spirit Management

    /// <summary>
    /// 添加精灵到队伍
    /// </summary>
    public bool AddSpirit(SpiritData spirit)
    {
        if (spirit == null || ownedSpirits.Contains(spirit))
            return false;

        ownedSpirits.Add(spirit);
        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 移除精灵
    /// </summary>
    public bool RemoveSpirit(SpiritData spirit)
    {
        if (spirit == null || !ownedSpirits.Contains(spirit))
            return false;

        // 如果精灵正在出场，先从出场列表移除
        if (deployedSpirits.Contains(spirit))
        {
            deployedSpirits.Remove(spirit);
        }

        ownedSpirits.Remove(spirit);
        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取所有拥有的精灵
    /// </summary>
    public List<SpiritData> GetAllSpirits()
    {
        return new List<SpiritData>(ownedSpirits);
    }

    /// <summary>
    /// 检查是否拥有指定精灵
    /// </summary>
    public bool HasSpirit(SpiritData spirit)
    {
        return spirit != null && ownedSpirits.Contains(spirit);
    }

    #endregion

    #region Deployed Spirits Management

    /// <summary>
    /// 让精灵出场
    /// </summary>
    public bool DeploySpirit(SpiritData spirit)
    {
        if (spirit == null)
            return false;

        // 必须拥有该精灵
        if (!ownedSpirits.Contains(spirit))
            return false;

        // 已经在出场列表中
        if (deployedSpirits.Contains(spirit))
            return false;

        // 检查出场数量限制
        if (deployedSpirits.Count >= maxDeployedSpirits)
            return false;

        deployedSpirits.Add(spirit);
        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 让精灵退场
    /// </summary>
    public bool RecallSpirit(SpiritData spirit)
    {
        if (spirit == null || !deployedSpirits.Contains(spirit))
            return false;

        deployedSpirits.Remove(spirit);
        OnDataChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取所有出场的精灵
    /// </summary>
    public List<SpiritData> GetDeployedSpirits()
    {
        return new List<SpiritData>(deployedSpirits);
    }

    /// <summary>
    /// 检查精灵是否已出场
    /// </summary>
    public bool IsDeployed(SpiritData spirit)
    {
        return spirit != null && deployedSpirits.Contains(spirit);
    }

    /// <summary>
    /// 获取出场精灵数量
    /// </summary>
    public int GetDeployedCount()
    {
        return deployedSpirits.Count;
    }

    /// <summary>
    /// 获取剩余可出场数量
    /// </summary>
    public int GetRemainingDeploySlots()
    {
        return Mathf.Max(0, maxDeployedSpirits - deployedSpirits.Count);
    }

    /// <summary>
    /// 设置出场精灵上限
    /// </summary>
    public void SetMaxDeployedSpirits(int max)
    {
        maxDeployedSpirits = Mathf.Clamp(max, 0, 6);

        // 如果当前出场数量超过新上限，移除多余的
        while (deployedSpirits.Count > maxDeployedSpirits)
        {
            deployedSpirits.RemoveAt(deployedSpirits.Count - 1);
        }

        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 获取出场精灵上限
    /// </summary>
    public int GetMaxDeployedSpirits()
    {
        return maxDeployedSpirits;
    }

    #endregion

    #region Utility

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        ownedItems.Clear();
        ownedSpirits.Clear();
        deployedSpirits.Clear();
        OnDataChanged?.Invoke();
    }

    #endregion
}

/// <summary>
/// 道具堆叠信息
/// </summary>
[Serializable]
public class ItemStack
{
    [SerializeField]
    private ItemData item;

    [SerializeField]
    private int count;

    public ItemData Item => item;
    public int Count
    {
        get => count;
        set => count = Mathf.Max(0, value);
    }

    public ItemStack(ItemData item, int count)
    {
        this.item = item;
        this.count = Mathf.Max(0, count);
    }
}
