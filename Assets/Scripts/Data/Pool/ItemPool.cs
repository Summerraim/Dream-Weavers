using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具数据对象池 - 用于存储和管理一组 ItemData
/// 可在 Project 面板中创建：右键 → Create → Data/Item Pool
/// </summary>
[CreateAssetMenu(menuName = "Data/Item Pool", fileName = "New Item Pool")]
public class ItemPool : ScriptableObject
{
    [Header("对象池配置")]
    [Tooltip("对象池的唯一ID")]
    public string PoolId;

    [Tooltip("对象池的显示名称")]
    public string DisplayName;

    [Tooltip("对象池描述")]
    [TextArea(2, 4)]
    public string Description;

    [Header("道具数据")]
    [Tooltip("对象池中包含的所有 ItemData（在面板中拖拽或使用下方工具自动收集）")]
    public List<ItemData> Items = new List<ItemData>();

    [Header("权重配置（可选）")]
    [Tooltip("是否启用权重系统（按权重随机）")]
    public bool UseWeights = false;

    [Tooltip("每个道具的出现权重（需与 Items 数量一致）")]
    public List<int> Weights = new List<int>();

    /// <summary>
    /// 获取对象池中的道具数量
    /// </summary>
    public int Count => Items?.Count ?? 0;

    /// <summary>
    /// 检查对象池是否为空
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 随机获取一个道具（均等概率）
    /// </summary>
    public ItemData GetRandomItem()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        int randomIndex = Random.Range(0, Items.Count);
        return Items[randomIndex];
    }

    /// <summary>
    /// 按权重随机获取一个道具
    /// </summary>
    public ItemData GetWeightedRandomItem()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (!UseWeights || Weights == null || Weights.Count != Items.Count)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Weights not configured properly, using uniform random.");
            return GetRandomItem();
        }

        int totalWeight = 0;
        foreach (int weight in Weights)
        {
            totalWeight += Mathf.Max(0, weight);
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Total weight is 0, using uniform random.");
            return GetRandomItem();
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        for (int i = 0; i < Items.Count; i++)
        {
            currentWeight += Mathf.Max(0, Weights[i]);
            if (randomValue < currentWeight)
            {
                return Items[i];
            }
        }
        return Items[Items.Count - 1];
    }

    /// <summary>
    /// 按索引获取道具
    /// </summary>
    public ItemData GetItemByIndex(int index)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (index < 0 || index >= Items.Count)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Index {index} out of range (0-{Items.Count - 1})");
            return null;
        }

        return Items[index];
    }

    /// <summary>
    /// 按名称查找道具（使用 DisplayName）
    /// </summary>
    public ItemData GetItemByName(string itemName)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        foreach (var item in Items)
        {
            if (item != null && item.DisplayName == itemName)
            {
                return item;
            }
        }

        Debug.LogWarning($"ItemPool [{DisplayName}]: Item '{itemName}' not found in pool.");
        return null;
    }

    /// <summary>
    /// 获取多个随机道具
    /// </summary>
    public List<ItemData> GetRandomItems(int count, bool allowDuplicates = false)
    {
        List<ItemData> result = new List<ItemData>();

        if (IsEmpty || count <= 0)
            return result;

        if (!allowDuplicates && count > Items.Count)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Requested {count} items but only {Items.Count} available without duplicates.");
            count = Items.Count;
        }

        if (allowDuplicates)
        {
            for (int i = 0; i < count; i++)
            {
                result.Add(GetRandomItem());
            }
        }
        else
        {
            List<ItemData> tempPool = new List<ItemData>(Items);
            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, tempPool.Count);
                result.Add(tempPool[randomIndex]);
                tempPool.RemoveAt(randomIndex);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有道具数据的只读列表
    /// </summary>
    public IReadOnlyList<ItemData> GetAllItems()
    {
        return Items.AsReadOnly();
    }

    /// <summary>
    /// 验证对象池配置
    /// </summary>
    public bool ValidatePool()
    {
        bool isValid = true;

        if (Items != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] == null)
                {
                    Debug.LogWarning($"ItemPool [{DisplayName}]: Item at index {i} is null!");
                    isValid = false;
                }
            }
        }

        if (UseWeights && Weights.Count != Items.Count)
        {
            Debug.LogWarning($"ItemPool [{DisplayName}]: Weights count ({Weights.Count}) doesn't match Items count ({Items.Count})!");
            isValid = false;
        }

        return isValid;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中：自动调整权重列表大小、并提供从 Resources/Items 自动收集的工具。
    /// </summary>
    private void OnValidate()
    {
        if (UseWeights && Weights != null && Items != null)
        {
            while (Weights.Count < Items.Count) Weights.Add(1);
            while (Weights.Count > Items.Count) Weights.RemoveAt(Weights.Count - 1);
        }
    }

    [ContextMenu("Collect Items From Resources/Items")]
    private void CollectFromResources()
    {
        var loaded = Resources.LoadAll<ItemData>("Items");
        Items.Clear();
        foreach (var it in loaded)
        {
            if (it != null) Items.Add(it);
        }
        Debug.Log($"[ItemPool] Collected {Items.Count} items from Resources/Items.");

        // 同步权重列表长度
        if (UseWeights)
        {
            while (Weights.Count < Items.Count) Weights.Add(1);
            while (Weights.Count > Items.Count) Weights.RemoveAt(Weights.Count - 1);
        }
    }

    /// <summary>
    /// 仅编辑器：从指定项目路径扫描 ItemData 并填充（例如 Assets/Data/items）。
    /// 不依赖 Resources，构建体更精简；运行时使用显式引用列表。
    /// </summary>
    [ContextMenu("Collect Items From Assets/Data/items")]
    private void CollectFromProjectPath()
    {
        // 仅在编辑器可用
        string targetFolder = "Assets/Data/items";
        Items.Clear();

        var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData", new[] { targetFolder });
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) Items.Add(item);
        }

        Debug.Log($"[ItemPool] Collected {Items.Count} items from {targetFolder}.");

        if (UseWeights)
        {
            while (Weights.Count < Items.Count) Weights.Add(1);
            while (Weights.Count > Items.Count) Weights.RemoveAt(Weights.Count - 1);
        }
    }
#endif
}
