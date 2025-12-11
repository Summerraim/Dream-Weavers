using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人数据对象池 - 用于存储和管理一组EnemyData
/// 可在Project面板中创建：右键 → Create → Data/Enemy Pool
/// </summary>
[CreateAssetMenu(menuName = "Data/Enemy Pool", fileName = "New Enemy Pool")]
public class EnemyPool : ScriptableObject
{
    [Header("对象池配置")]
    [Tooltip("对象池的唯一ID")]
    public string PoolId;

    [Tooltip("对象池的显示名称")]
    public string DisplayName;

    [Tooltip("对象池描述")]
    [TextArea(2, 4)]
    public string Description;

    [Header("敌人数据")]
    [Tooltip("对象池中包含的所有敌人数据")]
    public List<EnemyData> Enemies = new List<EnemyData>();

    [Header("精灵数据")]
    [Tooltip("与每个敌人对应的精灵数据（需与Enemies数量一致）")]
    public List<SpiritData> Spirits = new List<SpiritData>();

    [Header("权重配置（可选）")]
    [Tooltip("是否启用权重系统")]
    public bool UseWeights = false;

    [Tooltip("每个敌人的出现权重（需与Enemies数量一致）")]
    public List<int> Weights = new List<int>();

    /// <summary>
    /// 获取对象池中的敌人数量
    /// </summary>
    public int Count => Enemies?.Count ?? 0;

    /// <summary>
    /// 检查对象池是否为空
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 随机获取一个敌人数据（均等概率）
    /// </summary>
    public EnemyData GetRandomEnemy()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        int randomIndex = Random.Range(0, Enemies.Count);
        return Enemies[randomIndex];
    }

    /// <summary>
    /// 按权重随机获取一个敌人数据
    /// </summary>
    public EnemyData GetWeightedRandomEnemy()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (!UseWeights || Weights == null || Weights.Count != Enemies.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Weights not configured properly, using uniform random."
            );
            return GetRandomEnemy();
        }

        // 计算总权重
        int totalWeight = 0;
        foreach (int weight in Weights)
        {
            totalWeight += Mathf.Max(0, weight);
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Total weight is 0, using uniform random."
            );
            return GetRandomEnemy();
        }

        // 随机选择
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < Enemies.Count; i++)
        {
            currentWeight += Mathf.Max(0, Weights[i]);
            if (randomValue < currentWeight)
            {
                return Enemies[i];
            }
        }

        // 兜底返回最后一个
        return Enemies[Enemies.Count - 1];
    }

    /// <summary>
    /// 按索引获取敌人数据
    /// </summary>
    public EnemyData GetEnemyByIndex(int index)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (index < 0 || index >= Enemies.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Index {index} out of range (0-{Enemies.Count - 1})"
            );
            return null;
        }

        return Enemies[index];
    }

    /// <summary>
    /// 按名称查找敌人数据
    /// </summary>
    public EnemyData GetEnemyByName(string enemyName)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        foreach (var enemy in Enemies)
        {
            if (enemy != null && enemy.DisplayName == enemyName)
            {
                return enemy;
            }
        }

        Debug.LogWarning($"EnemyPool [{DisplayName}]: Enemy '{enemyName}' not found in pool.");
        return null;
    }

    /// <summary>
    /// 按索引获取对应的精灵数据
    /// </summary>
    public SpiritData GetSpiritByIndex(int index)
    {
        if (Spirits == null || Spirits.Count == 0)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Spirits list is empty!");
            return null;
        }

        if (index < 0 || index >= Spirits.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Spirit index {index} out of range (0-{Spirits.Count - 1})"
            );
            return null;
        }

        return Spirits[index];
    }

    /// <summary>
    /// 根据敌人数据获取对应的精灵数据
    /// </summary>
    public SpiritData GetSpiritForEnemy(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Enemy data is null!");
            return null;
        }

        int index = Enemies.IndexOf(enemyData);
        if (index == -1)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Enemy '{enemyData.DisplayName}' not found in pool!");
            return null;
        }

        return GetSpiritByIndex(index);
    }

    /// <summary>
    /// 随机获取一对敌人和精灵数据
    /// </summary>
    public (EnemyData enemy, SpiritData spirit) GetRandomEnemyWithSpirit()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return (null, null);
        }

        int randomIndex = Random.Range(0, Enemies.Count);
        return (Enemies[randomIndex], GetSpiritByIndex(randomIndex));
    }

    /// <summary>
    /// 按权重随机获取一对敌人和精灵数据
    /// </summary>
    public (EnemyData enemy, SpiritData spirit) GetWeightedRandomEnemyWithSpirit()
    {
        var enemy = GetWeightedRandomEnemy();
        if (enemy == null)
        {
            return (null, null);
        }

        var spirit = GetSpiritForEnemy(enemy);
        return (enemy, spirit);
    }

    /// <summary>
    /// 获取多个随机敌人（不重复）
    /// </summary>
    public List<EnemyData> GetRandomEnemies(int count, bool allowDuplicates = false)
    {
        List<EnemyData> result = new List<EnemyData>();

        if (IsEmpty)
        {
            Debug.LogWarning($"EnemyPool [{DisplayName}]: Pool is empty!");
            return result;
        }

        if (count <= 0)
            return result;

        if (!allowDuplicates && count > Enemies.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Requested {count} enemies but only {Enemies.Count} available without duplicates."
            );
            count = Enemies.Count;
        }

        if (allowDuplicates)
        {
            // 允许重复，直接随机抽取
            for (int i = 0; i < count; i++)
            {
                result.Add(GetRandomEnemy());
            }
        }
        else
        {
            // 不允许重复，使用洗牌算法
            List<EnemyData> tempPool = new List<EnemyData>(Enemies);
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
    /// 获取所有敌人数据的只读列表
    /// </summary>
    public IReadOnlyList<EnemyData> GetAllEnemies()
    {
        return Enemies.AsReadOnly();
    }

    /// <summary>
    /// 验证对象池配置
    /// </summary>
    public bool ValidatePool()
    {
        bool isValid = true;

        // 检查是否有空引用
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (Enemies[i] == null)
            {
                Debug.LogWarning($"EnemyPool [{DisplayName}]: Enemy at index {i} is null!");
                isValid = false;
            }
        }

        // 检查权重配置
        if (UseWeights && Weights.Count != Enemies.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Weights count ({Weights.Count}) doesn't match Enemies count ({Enemies.Count})!"
            );
            isValid = false;
        }

        // 检查Spirits列表大小
        if (Spirits != null && Spirits.Count != Enemies.Count)
        {
            Debug.LogWarning(
                $"EnemyPool [{DisplayName}]: Spirits count ({Spirits.Count}) doesn't match Enemies count ({Enemies.Count})!"
            );
            isValid = false;
        }

        return isValid;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中自动修复列表大小
    /// </summary>
    private void OnValidate()
    {
        // 自动调整权重列表大小以匹配敌人列表
        if (UseWeights && Weights != null && Enemies != null)
        {
            while (Weights.Count < Enemies.Count)
            {
                Weights.Add(1); // 默认权重为1
            }
            while (Weights.Count > Enemies.Count)
            {
                Weights.RemoveAt(Weights.Count - 1);
            }
        }

        // 自动调整精灵列表大小以匹配敌人列表
        if (Spirits != null && Enemies != null)
        {
            while (Spirits.Count < Enemies.Count)
            {
                Spirits.Add(null); // 默认为null，需要手动赋值
            }
            while (Spirits.Count > Enemies.Count)
            {
                Spirits.RemoveAt(Spirits.Count - 1);
            }
        }
    }
#endif
}
