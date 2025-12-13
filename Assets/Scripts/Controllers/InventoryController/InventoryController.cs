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

    // 当前战斗上下文（用于默认使用者=玩家）
    public BattleModel CurrentBattle { get; private set; }

    /// <summary>
    /// 由战斗控制器在初始化后调用，绑定战斗上下文
    /// </summary>
    public void BindBattle(BattleModel model)
    {
        CurrentBattle = model;
    }

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
        {
            Debug.LogWarning($"[Inventory] AddItem 参数非法: itemData={(itemData==null?"null":"ok")}, quantity={quantity}");
            return false;
        }

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
        if (!itemDictionary.ContainsKey(key))
        {
            itemDictionary[key] = newItem;
        }

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
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            return false;

        int remaining = quantity;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = items[i];
            if (item.data == null || item.data.ItemId != itemId)
                continue;

            int removeCount = Mathf.Min(item.quantity, remaining);
            item.quantity -= removeCount;
            remaining -= removeCount;

            if (item.quantity <= 0)
            {
                items.RemoveAt(i);
                if (itemDictionary.TryGetValue(itemId, out var dictItem) && dictItem == item)
                {
                    itemDictionary.Remove(itemId);
                }
                OnItemRemoved?.Invoke(item);
            }
            else
            {
                NotifyItemChanged(item);
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"移除物品失败: {itemId} 数量不足");
            return false;
        }

        // 若还有同类物品，确保字典指向其中之一
        if (!itemDictionary.ContainsKey(itemId))
        {
            var replacement = items.Find(inv => inv.data != null && inv.data.ItemId == itemId);
            if (replacement != null)
            {
                itemDictionary[itemId] = replacement;
            }
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"移除物品: {itemId} x{quantity}");
        return true;
    }

    /// <summary>
    /// 使用物品（通过ItemData查找）
    /// </summary>
    public void UseItem(ItemData itemData, IBattleUnit user = null, IBattleUnit target = null)
    {
        if (itemData == null)
        {
            Debug.LogError($"[Inventory] UseItem called with null ItemData");
            return;
        }

        Debug.Log($"[Inventory] UseItem called with ItemData: {itemData.DisplayName}");

        // 从items列表中查找匹配的InventoryItem
        InventoryItem foundItem = null;
        foreach (var item in items)
        {
            if (item != null && item.data != null && item.data == itemData)
            {
                foundItem = item;
                break;
            }
        }

        if (foundItem == null)
        {
            Debug.LogError($"[Inventory] Item not found in inventory: {itemData.DisplayName}");
            Debug.Log($"[Inventory] Available items: {string.Join(", ", items.ConvertAll(i => i?.data?.DisplayName ?? "null"))}");
            return;
        }

        Debug.Log($"[Inventory] Found item in inventory: {foundItem.DisplayName}, quantity={foundItem.quantity}");

        // 调用InventoryItem版本
        UseItem(foundItem, user, target);
    }

    /// <summary>
    /// 使用物品（通过ItemId查找）
    /// </summary>
    public void UseItem(string itemId, IBattleUnit user = null, IBattleUnit target = null)
    {
        Debug.Log($"[Inventory] UseItem called with itemId='{itemId}', user={user?.DisplayName}, target={target?.DisplayName}");
        Debug.Log($"[Inventory] itemDictionary contains {itemDictionary.Count} items");

        if (!itemDictionary.TryGetValue(itemId, out var item))
        {
            Debug.LogError($"[Inventory] Item not found in dictionary! itemId='{itemId}'");
            Debug.Log($"[Inventory] Available items in dictionary: {string.Join(", ", itemDictionary.Keys)}");
            return;
        }

        Debug.Log($"[Inventory] Found item in dictionary: {item.data?.DisplayName}");

        // 默认使用者为玩家（场上精灵）
        if (user == null)
        {
            user = CurrentBattle?.PlayerUnit;
        }

        // 若该道具需要单体目标但未提供，则提示并返回，让UI先进行选取
        if (item?.data != null && item.data.RequiresTargetSelection && target == null)
        {
            Debug.LogWarning($"[Inventory] 道具需要选择目标：{item.data.DisplayName}");
            return;
        }

        Debug.Log($"[Inventory] Calling UseItem(InventoryItem)");
        UseItem(item, user, target);
    }

    public void UseItem(InventoryItem item, IBattleUnit user = null, IBattleUnit target = null)
    {
        Debug.Log($"[Inventory] UseItem(InventoryItem) called: item={item?.DisplayName}, user={user?.DisplayName}, target={target?.DisplayName}");

        if (item == null || item.data == null)
        {
            Debug.LogError($"[Inventory] Item or item.data is null!");
            return;
        }

        Debug.Log($"[Inventory] Item data: {item.data.DisplayName}");

        // 默认使用者为玩家（场上精灵）。目标由调用方决定：
        // - 单体：UI 传入被点击的场上精灵
        // - 群体：传 null，由效果内部自行遍历
        if (user == null)
        {
            user = CurrentBattle?.PlayerUnit;
        }

        // SingleUnit 模式要求必须提供 target
        if (item.data.RequiresTargetSelection && target == null)
        {
            Debug.LogWarning($"[Inventory] 需要先选择一个目标来使用道具：{item.data.DisplayName}");
            return;
        }

        if (!item.data.CanUse(user, target))
        {
            Debug.LogWarning($"[Inventory] 道具当前不可使用: {item.data.DisplayName}");
            return;
        }

        Debug.Log($"[Inventory] Calling item.Use()");
        item.Use(user, target);

        Debug.Log($"[Inventory] item.Use() completed, item quantity now: {item.quantity}");

        // 使用后更新数量
        if (item.quantity <= 0)
        {
            string itemId = item.data.ItemId;
            items.Remove(item);
            if (itemDictionary.TryGetValue(itemId, out var dictItem) && dictItem == item)
            {
                itemDictionary.Remove(itemId);
            }
            // 若还有同类物品，重新指派字典引用
            if (!string.IsNullOrEmpty(itemId) && !itemDictionary.ContainsKey(itemId))
            {
                var replacement = items.Find(inv => inv.data != null && inv.data.ItemId == itemId);
                if (replacement != null)
                {
                    itemDictionary[itemId] = replacement;
                }
            }
            OnItemRemoved?.Invoke(item);
            OnInventoryChanged?.Invoke();
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
        return GetItemCount(itemId) >= quantity;
    }

    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 0;

        int total = 0;
        foreach (var item in items)
        {
            if (item.data != null && item.data.ItemId == itemId)
            {
                total += item.quantity;
            }
        }
        return total;
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
