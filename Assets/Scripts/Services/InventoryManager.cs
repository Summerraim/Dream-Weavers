using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包管理器（单例）
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region 单例
    public static InventoryManager Instance { get; private set; }
    #endregion

    [Header("背包设置")]
    public int maxSlots = 20; // 最大槽位数量
    public List<InventoryItem> items = new List<InventoryItem>(); // 背包物品列表

    [Header("事件")]
    public Action<InventoryItem> OnItemAdded; // 物品添加事件
    public Action<InventoryItem> OnItemRemoved; // 物品移除事件
    public Action OnInventoryChanged; // 背包变化事件

    private readonly Dictionary<string, InventoryItem> itemDictionary =
        new Dictionary<string, InventoryItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化背包（从存档加载或新建）
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        // 这里可以添加从存档加载背包数据的代码
        Debug.Log($"背包初始化，最大槽位: {maxSlots}");
    }

    #region 背包操作

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0)
            return false;

        string key = itemData.ItemId;
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[Inventory] 尝试添加缺少 ItemId 的物品");
            return false;
        }

        // 检查是否已有该物品且可堆叠
        if (itemDictionary.TryGetValue(key, out var existingItem))
        {
            // 尝试堆叠
            int maxStack = Mathf.Max(1, itemData.MaxStack);
            int available = Mathf.Max(0, maxStack - existingItem.quantity);
            if (available > 0)
            {
                int toAdd = Mathf.Min(available, quantity);
                existingItem.quantity += toAdd;
                quantity -= toAdd;
                NotifyItemChanged(existingItem);

                if (quantity <= 0)
                {
                    Debug.Log($"添加物品: {itemData.DisplayName} x{toAdd}");
                    return true;
                }
            }
        }

        // 创建新物品实例
        InventoryItem newItem = new InventoryItem(itemData, quantity);

        // 检查背包是否已满
        if (items.Count >= maxSlots)
        {
            Debug.LogWarning("背包已满！");
            return false;
        }

        // 添加到背包
        items.Add(newItem);
        itemDictionary[key] = newItem;

        // 触发事件
        OnItemAdded?.Invoke(newItem);
        OnInventoryChanged?.Invoke();

        Debug.Log($"添加物品: {itemData.DisplayName} x{quantity}");
        return true;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (!itemDictionary.ContainsKey(itemId))
            return false;

        InventoryItem item = itemDictionary[itemId];

        if (item.quantity < quantity)
            return false;

        item.quantity -= quantity;

        // 如果数量为0，完全移除
        if (item.quantity <= 0)
        {
            items.Remove(item);
            itemDictionary.Remove(itemId);
            OnItemRemoved?.Invoke(item);
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"移除物品: {item.data.DisplayName} x{quantity}");
        return true;
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    public void UseItem(string itemId, IBattleUnit user = null, IBattleUnit target = null)
    {
        if (!itemDictionary.TryGetValue(itemId, out var item))
            return;

        if (!item.data.CanUse(user, target))
        {
            Debug.LogWarning($"[Inventory] 道具当前不可使用: {item.data.DisplayName}");
            return;
        }

        item.Use(user, target);

        // 使用后更新数量
        if (item.quantity <= 0)
        {
            RemoveItem(itemId);
        }
        else
        {
            NotifyItemChanged(item);
        }
    }

    /// <summary>
    /// 检查是否有指定物品
    /// </summary>
    public bool HasItem(string itemId, int quantity = 1)
    {
        if (!itemDictionary.ContainsKey(itemId))
            return false;

        return itemDictionary[itemId].quantity >= quantity;
    }

    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemCount(string itemId)
    {
        if (!itemDictionary.ContainsKey(itemId))
            return 0;

        return itemDictionary[itemId].quantity;
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        itemDictionary.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("背包已清空");
    }

    /// <summary>
    /// 交换物品位置
    /// </summary>
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count || toIndex < 0 || toIndex >= items.Count)
            return;

        var temp = items[fromIndex];
        items[fromIndex] = items[toIndex];
        items[toIndex] = temp;

        OnInventoryChanged?.Invoke();
    }

    private void NotifyItemChanged(InventoryItem item)
    {
        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region 测试功能

    /// <summary>
    /// 添加测试物品（用于开发）
    /// </summary>
    [ContextMenu("添加测试物品")]
    public void AddTestItems()
    {
        // 创建测试物品数据
        ItemData healthPotion = ScriptableObject.CreateInstance<ItemData>();
        healthPotion.ConfigureRuntime(
            "health_potion",
            "生命药水",
            "恢复50点生命值",
            null,
            5,
            true
        );

        AddItem(healthPotion, 3);
    }

    #endregion
}
