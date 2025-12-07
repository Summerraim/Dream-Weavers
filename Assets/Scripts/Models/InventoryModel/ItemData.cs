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

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayNameOverride) ? name : displayNameOverride;

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
        // 执行挂载到道具上的效果列表（与 SkillData.Execute 逻辑一致）
        if (effects == null || effects.Count == 0)
        {
            Debug.Log($"[ItemData] Use called but no effects: {DisplayName}");
            return;
        }
        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;
            effect.Apply(user, target);
        }
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
