using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能数据对象池 - 用于存储和管理一组Skill
/// 可在Project面板中创建：右键 → Create → Data/Skill Pool
/// </summary>
[CreateAssetMenu(menuName = "Data/Skill Pool", fileName = "New Skill Pool")]
public class SkillPool : ScriptableObject
{
    [Header("对象池配置")]
    [Tooltip("对象池的唯一ID")]
    public string PoolId;

    [Tooltip("对象池的显示名称")]
    public string DisplayName;

    [Tooltip("对象池描述")]
    [TextArea(2, 4)]
    public string Description;

    [Header("技能数据")]
    [Tooltip("对象池中包含的所有技能数据（SkillData或实现ISkill的ScriptableObject）")]
    public List<ScriptableObject> Skills = new List<ScriptableObject>();

    [Header("权重配置（可选）")]
    [Tooltip("是否启用权重系统")]
    public bool UseWeights = false;

    [Tooltip("每个技能的出现权重（需与Skills数量一致）")]
    public List<int> Weights = new List<int>();

    [Header("标签系统（可选）")]
    [Tooltip("为每个技能添加标签，用于分类（如：攻击、治疗、控制等）")]
    public List<string> Tags = new List<string>();

    [Header("精灵映射（可选）")]
    [Tooltip("为每个技能指定对应的精灵DisplayName，多个精灵用逗号分隔（如：火精灵,冰精灵）（需与Skills数量一致）")]
    public List<string> SpiritNames = new List<string>();

    /// <summary>
    /// 获取对象池中的技能数量
    /// </summary>
    public int Count => Skills?.Count ?? 0;

    /// <summary>
    /// 检查对象池是否为空
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 随机获取一个技能（均等概率）
    /// </summary>
    public ScriptableObject GetRandomSkill()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        int randomIndex = Random.Range(0, Skills.Count);
        return Skills[randomIndex];
    }

    /// <summary>
    /// 随机获取一个ISkill接口实例（均等概率）
    /// </summary>
    public ISkill GetRandomISkill()
    {
        var skillObj = GetRandomSkill();
        if (skillObj == null)
            return null;

        // 尝试直接转换为ISkill
        if (skillObj is ISkill skill)
            return skill;

        // 如果是SkillData，使用Skill类包装
        if (skillObj is SkillData skillData)
            return new Skill(skillData);

        Debug.LogWarning($"SkillPool [{DisplayName}]: Skill object is not compatible with ISkill interface!");
        return null;
    }

    /// <summary>
    /// 按权重随机获取一个技能
    /// </summary>
    public ScriptableObject GetWeightedRandomSkill()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (!UseWeights || Weights == null || Weights.Count != Skills.Count)
        {
            Debug.LogWarning(
                $"SkillPool [{DisplayName}]: Weights not configured properly, using uniform random."
            );
            return GetRandomSkill();
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
                $"SkillPool [{DisplayName}]: Total weight is 0, using uniform random."
            );
            return GetRandomSkill();
        }

        // 随机选择
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < Skills.Count; i++)
        {
            currentWeight += Mathf.Max(0, Weights[i]);
            if (randomValue < currentWeight)
            {
                return Skills[i];
            }
        }

        // 兜底返回最后一个
        return Skills[Skills.Count - 1];
    }

    /// <summary>
    /// 按权重随机获取一个ISkill接口实例
    /// </summary>
    public ISkill GetWeightedRandomISkill()
    {
        var skillObj = GetWeightedRandomSkill();
        if (skillObj == null)
            return null;

        // 尝试直接转换为ISkill
        if (skillObj is ISkill skill)
            return skill;

        // 如果是SkillData，使用Skill类包装
        if (skillObj is SkillData skillData)
            return new Skill(skillData);

        Debug.LogWarning($"SkillPool [{DisplayName}]: Skill object is not compatible with ISkill interface!");
        return null;
    }

    /// <summary>
    /// 按索引获取技能
    /// </summary>
    public ScriptableObject GetSkillByIndex(int index)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        if (index < 0 || index >= Skills.Count)
        {
            Debug.LogWarning(
                $"SkillPool [{DisplayName}]: Index {index} out of range (0-{Skills.Count - 1})"
            );
            return null;
        }

        return Skills[index];
    }

    /// <summary>
    /// 按名称查找技能
    /// </summary>
    public ScriptableObject GetSkillByName(string skillName)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Pool is empty!");
            return null;
        }

        foreach (var skillObj in Skills)
        {
            if (skillObj == null)
                continue;

            // 尝试通过ISkill接口获取名称
            if (skillObj is ISkill skill && skill.DisplayName == skillName)
            {
                return skillObj;
            }

            // 尝试直接通过ScriptableObject名称匹配
            if (skillObj.name == skillName)
            {
                return skillObj;
            }
        }

        Debug.LogWarning($"SkillPool [{DisplayName}]: Skill '{skillName}' not found in pool.");
        return null;
    }

    /// <summary>
    /// 按标签查找所有技能
    /// </summary>
    public List<ScriptableObject> GetSkillsByTag(string tag)
    {
        List<ScriptableObject> result = new List<ScriptableObject>();

        if (IsEmpty || Tags == null || Tags.Count != Skills.Count)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Tags not configured properly!");
            return result;
        }

        for (int i = 0; i < Skills.Count; i++)
        {
            if (Tags[i] == tag && Skills[i] != null)
            {
                result.Add(Skills[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 按精灵名称获取对应的技能（支持一个技能对应多个精灵，用逗号分隔）
    /// </summary>
    /// <param name="spiritDisplayName">精灵的DisplayName</param>
    /// <returns>该精灵对应的技能，如果没有找到则返回null</returns>
    public ScriptableObject GetSkillBySpiritName(string spiritDisplayName)
    {
        if (IsEmpty || SpiritNames == null || SpiritNames.Count != Skills.Count)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: SpiritNames not configured properly!");
            return null;
        }

        for (int i = 0; i < Skills.Count; i++)
        {
            if (string.IsNullOrEmpty(SpiritNames[i]) || Skills[i] == null)
                continue;

            // 支持逗号分隔的多个精灵名称
            string[] spirits = SpiritNames[i].Split(',');
            foreach (var spirit in spirits)
            {
                string trimmedName = spirit.Trim();
                if (trimmedName == spiritDisplayName)
                {
                    return Skills[i];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 按精灵名称获取所有对应的技能（一个精灵可能有多个技能）
    /// </summary>
    /// <param name="spiritDisplayName">精灵的DisplayName</param>
    /// <returns>该精灵对应的所有技能列表</returns>
    public List<ScriptableObject> GetAllSkillsBySpiritName(string spiritDisplayName)
    {
        List<ScriptableObject> result = new List<ScriptableObject>();

        if (IsEmpty || SpiritNames == null || SpiritNames.Count != Skills.Count)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: SpiritNames not configured properly!");
            return result;
        }

        for (int i = 0; i < Skills.Count; i++)
        {
            if (string.IsNullOrEmpty(SpiritNames[i]) || Skills[i] == null)
                continue;

            // 支持逗号分隔的多个精灵名称
            string[] spirits = SpiritNames[i].Split(',');
            foreach (var spirit in spirits)
            {
                string trimmedName = spirit.Trim();
                if (trimmedName == spiritDisplayName)
                {
                    result.Add(Skills[i]);
                    break; // 避免重复添加同一个技能
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 按精灵名称获取对应的ISkill接口实例
    /// </summary>
    /// <param name="spiritDisplayName">精灵的DisplayName</param>
    /// <returns>该精灵对应的ISkill实例</returns>
    public ISkill GetISkillBySpiritName(string spiritDisplayName)
    {
        var skillObj = GetSkillBySpiritName(spiritDisplayName);
        if (skillObj == null)
            return null;

        if (skillObj is ISkill skill)
            return skill;

        if (skillObj is SkillData skillData)
            return new Skill(skillData);

        Debug.LogWarning($"SkillPool [{DisplayName}]: Skill object is not compatible with ISkill interface!");
        return null;
    }

    /// <summary>
    /// 获取所有精灵名称与技能的映射（支持一个技能对应多个精灵）
    /// </summary>
    /// <returns>精灵名称到技能的字典（每个精灵只返回第一个匹配的技能）</returns>
    public Dictionary<string, ScriptableObject> GetSpiritSkillMapping()
    {
        var mapping = new Dictionary<string, ScriptableObject>();

        if (IsEmpty || SpiritNames == null || SpiritNames.Count != Skills.Count)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: SpiritNames not configured properly!");
            return mapping;
        }

        for (int i = 0; i < Skills.Count; i++)
        {
            if (string.IsNullOrEmpty(SpiritNames[i]) || Skills[i] == null)
                continue;

            // 支持逗号分隔的多个精灵名称
            string[] spirits = SpiritNames[i].Split(',');
            foreach (var spirit in spirits)
            {
                string trimmedName = spirit.Trim();
                if (!string.IsNullOrEmpty(trimmedName) && !mapping.ContainsKey(trimmedName))
                {
                    mapping[trimmedName] = Skills[i];
                }
            }
        }

        return mapping;
    }

    /// <summary>
    /// 获取多个随机技能（不重复）
    /// </summary>
    public List<ScriptableObject> GetRandomSkills(int count, bool allowDuplicates = false)
    {
        List<ScriptableObject> result = new List<ScriptableObject>();

        if (IsEmpty)
        {
            Debug.LogWarning($"SkillPool [{DisplayName}]: Pool is empty!");
            return result;
        }

        if (count <= 0)
            return result;

        if (!allowDuplicates && count > Skills.Count)
        {
            Debug.LogWarning(
                $"SkillPool [{DisplayName}]: Requested {count} skills but only {Skills.Count} available without duplicates."
            );
            count = Skills.Count;
        }

        if (allowDuplicates)
        {
            // 允许重复，直接随机抽取
            for (int i = 0; i < count; i++)
            {
                result.Add(GetRandomSkill());
            }
        }
        else
        {
            // 不允许重复，使用洗牌算法
            List<ScriptableObject> tempPool = new List<ScriptableObject>(Skills);
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
    /// 获取多个随机ISkill接口实例（不重复）
    /// </summary>
    public List<ISkill> GetRandomISkills(int count, bool allowDuplicates = false)
    {
        List<ISkill> result = new List<ISkill>();
        var skillObjects = GetRandomSkills(count, allowDuplicates);

        foreach (var skillObj in skillObjects)
        {
            if (skillObj == null)
                continue;

            // 尝试转换为ISkill
            if (skillObj is ISkill skill)
            {
                result.Add(skill);
            }
            else if (skillObj is SkillData skillData)
            {
                result.Add(new Skill(skillData));
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有技能数据的只读列表
    /// </summary>
    public IReadOnlyList<ScriptableObject> GetAllSkills()
    {
        return Skills.AsReadOnly();
    }

    /// <summary>
    /// 获取所有技能作为ISkill接口列表
    /// </summary>
    public List<ISkill> GetAllISkills()
    {
        List<ISkill> result = new List<ISkill>();

        foreach (var skillObj in Skills)
        {
            if (skillObj == null)
                continue;

            if (skillObj is ISkill skill)
            {
                result.Add(skill);
            }
            else if (skillObj is SkillData skillData)
            {
                result.Add(new Skill(skillData));
            }
        }

        return result;
    }

    /// <summary>
    /// 验证对象池配置
    /// </summary>
    public bool ValidatePool()
    {
        bool isValid = true;

        // 检查是否有空引用
        for (int i = 0; i < Skills.Count; i++)
        {
            if (Skills[i] == null)
            {
                Debug.LogWarning($"SkillPool [{DisplayName}]: Skill at index {i} is null!");
                isValid = false;
            }
            else
            {
                // 检查是否实现ISkill接口或为SkillData
                if (!(Skills[i] is ISkill) && !(Skills[i] is SkillData))
                {
                    Debug.LogWarning(
                        $"SkillPool [{DisplayName}]: Skill at index {i} ({Skills[i].name}) is not ISkill or SkillData!"
                    );
                    isValid = false;
                }
            }
        }

        // 检查权重配置
        if (UseWeights && Weights.Count != Skills.Count)
        {
            Debug.LogWarning(
                $"SkillPool [{DisplayName}]: Weights count ({Weights.Count}) doesn't match Skills count ({Skills.Count})!"
            );
            isValid = false;
        }

        // 检查标签配置
        if (Tags != null && Tags.Count > 0 && Tags.Count != Skills.Count)
        {
            Debug.LogWarning(
                $"SkillPool [{DisplayName}]: Tags count ({Tags.Count}) doesn't match Skills count ({Skills.Count})!"
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
        // 自动调整权重列表大小以匹配技能列表
        if (UseWeights && Weights != null && Skills != null)
        {
            while (Weights.Count < Skills.Count)
            {
                Weights.Add(1); // 默认权重为1
            }
            while (Weights.Count > Skills.Count)
            {
                Weights.RemoveAt(Weights.Count - 1);
            }
        }

        // 自动调整标签列表大小以匹配技能列表
        if (Tags != null && Skills != null)
        {
            while (Tags.Count < Skills.Count)
            {
                Tags.Add(""); // 默认为空标签
            }
            while (Tags.Count > Skills.Count)
            {
                Tags.RemoveAt(Tags.Count - 1);
            }
        }

        // 自动调整精灵名称列表大小以匹配技能列表
        if (SpiritNames != null && Skills != null)
        {
            while (SpiritNames.Count < Skills.Count)
            {
                SpiritNames.Add(""); // 默认为空
            }
            while (SpiritNames.Count > Skills.Count)
            {
                SpiritNames.RemoveAt(SpiritNames.Count - 1);
            }
        }
    }
#endif
}
