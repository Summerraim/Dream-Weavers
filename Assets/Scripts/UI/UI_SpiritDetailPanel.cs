using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spirit名称与图标大小的映射
/// </summary>
[Serializable]
public class SpiritIconSizeMapping
{
    public string spiritName; // Spirit名称
    public Vector2 iconSize; // 对应的图标大小
}

/// <summary>
/// Spirit详情面板 - 显示选中Spirit的详细信息
/// 包括名称、属性、技能列表等
/// </summary>
public class UI_SpiritDetailPanel : MonoBehaviour
{
    [Header("Spirit基本信息")]
    [SerializeField]
    private Image spiritIcon; // Spirit图标

    [SerializeField]
    private TMP_Text spiritNameText; // Spirit名称

    [SerializeField]
    private TMP_Text statsText; // 属性文本 (HP, MP, ATK, DEF)

    [Header("图标大小设置")]
    [SerializeField]
    private Vector2 defaultIconSize = new Vector2(100, 100); // 默认图标大小

    [SerializeField]
    private bool useCustomSizeMapping = true; // 是否使用自定义大小映射

    [SerializeField]
    private List<SpiritIconSizeMapping> iconSizeMappings = new List<SpiritIconSizeMapping>(); // 名称-大小映射表

    [Header("技能列表")]
    [SerializeField]
    private Transform skillListContainer; // 技能列表容器

    [SerializeField]
    private GameObject skillItemPrefab; // 技能项预制体（可选）

    [Header("空状态显示")]
    [SerializeField]
    private GameObject emptyStatePanel; // 未选中任何Spirit时显示的面板

    [SerializeField]
    private TMP_Text emptyStateText; // 提示文本

    private SpiritData currentSpirit; // 当前显示的Spirit

    private void Awake()
    {
        // 初始显示空状态
        ShowEmptyState();
    }

    /// <summary>
    /// 显示Spirit详情
    /// </summary>
    public void ShowSpiritDetails(SpiritData spirit)
    {
        if (spirit == null)
        {
            ShowEmptyState();
            return;
        }

        currentSpirit = spirit;

        // 隐藏空状态面板
        if (emptyStatePanel != null)
            emptyStatePanel.SetActive(false);

        // 显示Spirit图标
        if (spiritIcon != null)
        {
            spiritIcon.enabled = true;
            spiritIcon.sprite = spirit.Image;

            // 调整图标大小
            AdjustIconSize(spirit);
        }

        // 显示Spirit名称
        if (spiritNameText != null)
        {
            spiritNameText.text = spirit.DisplayName;
        }

        // 显示属性
        if (statsText != null)
        {
            statsText.text =
                $"HP: {spirit.MaxHP}\n"
                + $"MP: {spirit.MaxMana}\n"
                + $"ATK: {spirit.Damage}\n"
                + $"DEF: {spirit.Defense}";
        }

        // 显示技能列表
        DisplaySkills(spirit);

        Debug.Log($"[UI_SpiritDetailPanel] 显示Spirit详情: {spirit.DisplayName}");
    }

    /// <summary>
    /// 显示技能列表
    /// </summary>
    private void DisplaySkills(SpiritData spirit)
    {
        if (skillListContainer == null)
        {
            Debug.LogWarning("[UI_SpiritDetailPanel] skillListContainer is null!");
            return;
        }

        // 清除现有技能项
        foreach (Transform child in skillListContainer)
        {
            Destroy(child.gameObject);
        }

        // 检查Spirit是否有技能
        if (spirit.Skills == null || spirit.Skills.Length == 0)
        {
            CreateSkillItem("无技能", "此Spirit暂无技能");
            return;
        }

        // 创建技能项
        for (int i = 0; i < spirit.Skills.Length; i++)
        {
            var skillObj = spirit.Skills[i];
            if (skillObj == null)
            {
                Debug.LogWarning($"[UI_SpiritDetailPanel] Skill[{i}] is null!");
                continue;
            }

            // 尝试转换为 ISkill 接口
            if (skillObj is ISkill skill)
            {
                string skillName = skill.DisplayName;
                string skillInfo = $"消耗: {skill.ManaCost} MP";

                // 添加描述（如果有）
                if (!string.IsNullOrEmpty(skill.Description))
                {
                    skillInfo += $"\n{skill.Description}";
                }

                CreateSkillItem(skillName, skillInfo);
            }
            // 尝试转换为 SkillData
            else if (skillObj is SkillData skillData)
            {
                string skillName = skillData.DisplayName;
                string skillInfo = $"消耗: {skillData.Mana} MP";

                // 添加描述（如果有）
                if (!string.IsNullOrEmpty(skillData.Description))
                {
                    skillInfo += $"\n{skillData.Description}";
                }

                CreateSkillItem(skillName, skillInfo);
            }
            else
            {
                // 如果无法转换，显示基本信息
                CreateSkillItem(skillObj.name, "未知技能类型");
            }
        }

        Debug.Log($"[UI_SpiritDetailPanel] 显示 {spirit.Skills.Length} 个技能");
    }

    /// <summary>
    /// 创建技能项UI
    /// </summary>
    private void CreateSkillItem(string skillName, string skillInfo)
    {
        GameObject skillItem;

        // 如果有预制体，使用预制体
        if (skillItemPrefab != null)
        {
            skillItem = Instantiate(skillItemPrefab, skillListContainer);

            // 尝试在预制体中查找Text组件并设置文本
            var nameText = skillItem.transform.Find("SkillName")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = skillName;

            var infoText = skillItem.transform.Find("SkillInfo")?.GetComponent<TMP_Text>();
            if (infoText != null)
                infoText.text = skillInfo;
        }
        else { }
    }

    /// <summary>
    /// 显示空状态（未选中任何Spirit）
    /// </summary>
    private void ShowEmptyState()
    {
        currentSpirit = null;

        // 隐藏Spirit信息
        if (spiritIcon != null)
            spiritIcon.enabled = false;

        if (spiritNameText != null)
            spiritNameText.text = "";

        if (statsText != null)
            statsText.text = "";

        // 清空技能列表
        if (skillListContainer != null)
        {
            foreach (Transform child in skillListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 显示空状态面板
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(true);
        }

        if (emptyStateText != null)
        {
            emptyStateText.text = "点击一个Spirit查看详情";
        }

        Debug.Log("[UI_SpiritDetailPanel] 显示空状态");
    }

    /// <summary>
    /// 清空详情显示
    /// </summary>
    public void Clear()
    {
        ShowEmptyState();
    }

    /// <summary>
    /// 获取当前显示的Spirit
    /// </summary>
    public SpiritData GetCurrentSpirit()
    {
        return currentSpirit;
    }

    /// <summary>
    /// 调整Spirit图标大小
    /// </summary>
    private void AdjustIconSize(SpiritData spirit)
    {
        if (spiritIcon == null || spirit == null)
            return;

        Vector2 targetSize;

        // 优先级1: 查找自定义映射表
        if (useCustomSizeMapping && TryGetMappedSize(spirit.DisplayName, out Vector2 mappedSize))
        {
            targetSize = mappedSize;
            Debug.Log(
                $"[UI_SpiritDetailPanel] Using mapped size {targetSize} for {spirit.DisplayName}"
            );
        }
        // 优先级3: 使用默认大小
        else
        {
            targetSize = defaultIconSize;
            Debug.Log(
                $"[UI_SpiritDetailPanel] Using default size {targetSize} for {spirit.DisplayName}"
            );
        }

        SetIconSize(targetSize);
    }

    /// <summary>
    /// 尝试从映射表中获取指定名称的图标大小
    /// </summary>
    private bool TryGetMappedSize(string spiritName, out Vector2 size)
    {
        if (iconSizeMappings != null && iconSizeMappings.Count > 0)
        {
            foreach (var mapping in iconSizeMappings)
            {
                if (mapping.spiritName == spiritName)
                {
                    size = mapping.iconSize;
                    return true;
                }
            }
        }

        size = Vector2.zero;
        return false;
    }

    /// <summary>
    /// 设置图标大小
    /// </summary>
    private void SetIconSize(Vector2 size)
    {
        if (spiritIcon == null)
            return;

        var rectTransform = spiritIcon.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
    }

    /// <summary>
    /// 公开方法：手动设置图标大小
    /// 可以从外部调用以自定义图标大小
    /// </summary>
    public void SetCustomIconSize(Vector2 size)
    {
        SetIconSize(size);
    }

    /// <summary>
    /// 公开方法：重置图标到默认大小
    /// </summary>
    public void ResetIconSize()
    {
        SetIconSize(defaultIconSize);
    }

    /// <summary>
    /// 公开方法：添加或更新Spirit名称-图标大小映射
    /// </summary>
    /// <param name="spiritName">Spirit名称</param>
    /// <param name="iconSize">图标大小</param>
    public void AddOrUpdateSizeMapping(string spiritName, Vector2 iconSize)
    {
        if (string.IsNullOrEmpty(spiritName))
            return;

        // 查找是否已存在
        var existing = iconSizeMappings.Find(m => m.spiritName == spiritName);
        if (existing != null)
        {
            // 更新现有映射
            existing.iconSize = iconSize;
            Debug.Log(
                $"[UI_SpiritDetailPanel] Updated size mapping for {spiritName} to {iconSize}"
            );
        }
        else
        {
            // 添加新映射
            iconSizeMappings.Add(
                new SpiritIconSizeMapping { spiritName = spiritName, iconSize = iconSize }
            );
            Debug.Log($"[UI_SpiritDetailPanel] Added size mapping for {spiritName}: {iconSize}");
        }
    }

    /// <summary>
    /// 公开方法：移除Spirit名称-图标大小映射
    /// </summary>
    public void RemoveSizeMapping(string spiritName)
    {
        if (string.IsNullOrEmpty(spiritName))
            return;

        iconSizeMappings.RemoveAll(m => m.spiritName == spiritName);
        Debug.Log($"[UI_SpiritDetailPanel] Removed size mapping for {spiritName}");
    }

    /// <summary>
    /// 公开方法：清空所有映射
    /// </summary>
    public void ClearAllMappings()
    {
        iconSizeMappings.Clear();
        Debug.Log("[UI_SpiritDetailPanel] Cleared all size mappings");
    }
}
