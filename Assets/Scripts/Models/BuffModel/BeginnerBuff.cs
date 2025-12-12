using UnityEngine;

/// <summary>
/// 初心者Buff：第一次技能释放不消耗MP且不进入冷却
/// 注意：此Buff需要在BattleModel的技能释放逻辑中检查
/// 当检测到单位拥有此Buff且未使用过时，技能释放后不消耗MP且不设置冷却
/// </summary>
public class BeginnerBuff : Buff
{
    public override string DisplayName => "初心者";
    public override string Description => "第一次技能释放不消耗MP且不进入冷却";

    private bool hasUsedFirstSkill;

    public bool HasUsedFirstSkill => hasUsedFirstSkill;

    public BeginnerBuff(IBattleUnit owner)
        : base(owner, -1) // 永久Buff
    {
        this.hasUsedFirstSkill = false;
    }

    public override void OnApplied()
    {
        Debug.Log($"BeginnerBuff: Applied to {Owner?.DisplayName}");
    }

    /// <summary>
    /// 检查是否可以触发首次技能免费
    /// 在BattleModel的技能释放前调用此方法
    /// </summary>
    public bool CanTriggerFreeSkill()
    {
        return !hasUsedFirstSkill;
    }

    /// <summary>
    /// 标记已使用首次技能
    /// 在BattleModel的技能释放后调用此方法
    /// </summary>
    public void MarkFirstSkillUsed()
    {
        if (!hasUsedFirstSkill)
        {
            hasUsedFirstSkill = true;
            Debug.Log($"BeginnerBuff: {Owner?.DisplayName} used first free skill");
        }
    }

    public override void OnRemoved()
    {
        Debug.Log($"BeginnerBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
