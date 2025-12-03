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

    // 使用物品（适配 IItem）
    public void Use(IBattleUnit user = null, IBattleUnit target = null)
    {
        if (data == null)
            return;

        // 前置校验（与 UI 保持一致）
        if (!data.CanUse(user, target))
            return;

        data.Use(user, target);

        if (data.RemoveOnUse)
        {
            quantity--;
            if (quantity < 0) quantity = 0;
        }
    }
}
