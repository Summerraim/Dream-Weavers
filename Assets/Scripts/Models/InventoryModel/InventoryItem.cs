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
    public int quantity; // 数量
    public int slotIndex; // 所在槽位索引

    // 构造函数
    public InventoryItem(ItemData itemData, int amount = 1)
    {
        data = itemData;
        quantity = amount;
        slotIndex = -1; // 初始未分配槽位
    }

    // 判断是否可堆叠
    public bool CanStackWith(InventoryItem other)
    {
        return other != null
            && other.data != null
            && data != null
            && other.data.itemId == data.itemId
            && quantity < data.maxStack;
    }

    // 尝试堆叠
    public bool TryStackWith(InventoryItem other, out int remaining)
    {
        remaining = 0;

        if (!CanStackWith(other))
            return false;

        int total = quantity + other.quantity;
        if (total <= data.maxStack)
        {
            quantity = total;
            other.quantity = 0;
            return true;
        }
        else
        {
            quantity = data.maxStack;
            remaining = total - data.maxStack;
            return true;
        }
    }

    // 使用物品
    public void Use()
    {
        if (data != null)
        {
            data.Use();

            // 如果是消耗品，使用后减少数量
            if (data.consumable)
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
