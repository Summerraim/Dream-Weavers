using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品基础数据（ScriptableObject，可在编辑器中创建）
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Data/ItemData")]

public class ItemData : ScriptableObject, IItem
{
    [SerializeField]
    private string itemId;

    [SerializeField]
    private string displayNameOverride;

    [field: SerializeField]
    public string Description { get; private set; }

    [field: SerializeField]
    public int MaxStack { get; private set; } = 1;

    [field: SerializeField]
    public bool RemoveOnUse { get; private set; } = true;

    [field: SerializeField]
    public Sprite Icon { get; private set; }

    [SerializeField]
   
    private List<Effect> effects = new();

    [Header("目标模式（用于UI是否需要点选目标）")]
    [SerializeField]
    private TargetingMode targetingMode = TargetingMode.SingleUnit;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayNameOverride) ? name : displayNameOverride;

    /// <summary>
    /// 当前道具的目标模式（用于上层UI/控制器判断是否需要选择target）
    /// </summary>
    public TargetingMode TargetMode => targetingMode;

    /// <summary>
    /// 是否需要玩家选择一个单体目标
    /// </summary>
    public bool RequiresTargetSelection => targetingMode == TargetingMode.SingleUnit;

    public bool CanUse(IBattleUnit user, IBattleUnit target)
    {
        if (user != null && user.IsDead)
            return false;
        return OnCanUse(user, target);
    }

    public void Use(IBattleUnit user, IBattleUnit target)
    {
        OnUse(user, target);
    }

    protected virtual bool OnCanUse(IBattleUnit user, IBattleUnit target)
    {
        return true;
    }

    protected virtual void OnUse(IBattleUnit user, IBattleUnit target)
    {
        Debug.Log($"[ItemData] OnUse called: {DisplayName}, user={user?.DisplayName}, target={target?.DisplayName}");
        Debug.Log($"[ItemData] Effects count: {(effects != null ? effects.Count : 0)}");

        // 执行挂载到道具上的效果列表（与 SkillData.Execute 逻辑一致）
        if (effects == null || effects.Count == 0)
        {
            Debug.LogWarning($"[ItemData] Use called but no effects: {DisplayName}");
            return;
        }

        // 目标模式仅用于提示UI/调用层：
        // - SingleUnit 期望传入一个有效 target；
        // - 其他模式允许 target=null，由 Effect 内部处理单体/群体分发。
        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
            {
                Debug.LogWarning($"[ItemData] Effect {i} is null");
                continue;
            }
            Debug.Log($"[ItemData] Applying effect {i}: {effect.GetType().Name}");
            effect.Apply(user, target);
        }

        Debug.Log($"[ItemData] OnUse finished for {DisplayName}");
    }

    /// <summary>
    /// 运行时配置道具（用于测试或动态生成）
    /// </summary>
    public void ConfigureRuntime(
        string id,
        string displayName,
        string description,
        Sprite icon,
        int maxStack,   
        bool removeOnUse
    )
    {
        itemId = id;
        displayNameOverride = displayName;
        Description = description;
        Icon = icon;
        MaxStack = Mathf.Max(1, maxStack);
        RemoveOnUse = removeOnUse;
    }
}
