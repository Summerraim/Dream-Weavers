using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品基础数据（ScriptableObject，可在编辑器中创建）
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("基本信息")]
    public string itemId; // 物品唯一ID
    public string itemName; // 物品名称

    [TextArea(3, 5)]
    public string description; // 物品描述
    public Sprite icon; // 物品图标
    public GameObject prefab; // 物品预制体（如果是可放置物品）

    [Header("物品属性")]
    public ItemType itemType; // 物品类型
    public int maxStack = 1; // 最大堆叠数量
    public float weight = 0.1f; // 重量
    public int value = 1; // 价值p

    [Header("使用效果")]
    public bool consumable = false; // 是否可消耗
    public float healthEffect = 0f; // 生命值影响
    public float manaEffect = 0f; // 魔法值影响

    public enum ItemType
    {
        Material, // 材料
        Consumable, // 消耗品
        Weapon, // 武器
        Armor, // 护甲
        Quest, // 任务物品
        Misc, // 杂项
    }

    public virtual void Use()
    {
        // 基础使用效果
        Debug.Log($"使用物品: {itemName}");

        // 消耗品效果
        if (consumable)
        {
            // 这里可以调用玩家状态管理器
            if (healthEffect != 0)
                Debug.Log($"生命值变化: {healthEffect}");

            if (manaEffect != 0)
                Debug.Log($"魔法值变化: {manaEffect}");
        }
    }
}
