using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包中的单个物品实例
/// </summary>
[System.Serializable]
public class InventoryItem 
{
    public ItemData data; // 物品数据
    public SpiritData spiritData; // 敌人数据
    public int quantity; // 数量
    public int slotIndex; // 所在槽位索引
    //  public int HP { get; private set; }
    // public int Mana { get; private set; }
    // public int Damage { get; private set; }
    // public int Defense { get; private set; }

    // public string DisplayName =>
    //     data != null
    //         ? (string.IsNullOrWhiteSpace(data.itemName) ? data.name : data.itemName)
    //         : string.Empty;

    // public int MaxHP => spiritData?.MaxHP ?? 0;
    // public int MaxMana => spiritData?.MaxMana ?? 0;

    // 构造函数
    public InventoryItem(ItemData itemData, int amount = 1)
    {
        data = itemData;
        quantity = amount;
        slotIndex = -1; // 初始未分配槽位
    }

    // 只读访问器（便于 UI/逻辑统一访问，类似 Skill 的包装）
    public string DisplayName => data?.DisplayName ?? string.Empty;
    public string Description => data?.Description ?? string.Empty;
    public Sprite Icon => data?.Icon;
    public int MaxStack => data?.MaxStack ?? 1;

    // 判断是否可堆叠
    public bool CanStackWith(InventoryItem other)
    {
        return other != null
            && other.data != null
            && data != null
            && other.data.ItemId == data.ItemId
            && quantity < data.MaxStack;
    }

    // 尝试堆叠
    public bool TryStackWith(InventoryItem other, out int remaining)
    {
        remaining = 0;

        if (!CanStackWith(other))
            return false;

        int total = quantity + other.quantity;
        if (total <= data.MaxStack)
        {
            quantity = total;
            other.quantity = 0;
            return true;
        }
        else
        {
            quantity = data.MaxStack;
            remaining = total - data.MaxStack;
            return true;
        }
    }

    // 可用性转发（让外部可直接对实例询问可用性）
    public bool CanUse(IBattleUnit user, IBattleUnit target)
    {
        return data != null && data.CanUse(user, target);
    }

    // 使用物品
    public void Use(IBattleUnit user, IBattleUnit target)
    {
        if (data != null)
        {
            data.Use(user,target);

            // 如果是消耗品，使用后减少数量
            if (data.RemoveOnUse)
            {
                quantity--;
                if (quantity <= 0)
                {
                    // 物品用完，从背包中移除（由背包管理器处理）
                }
            }
        }
    }
}
