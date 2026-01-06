using UnityEngine;

/// <summary>
/// 初心者 Buff：每场战斗第一次技能释放不消耗 MP，且该技能不进入冷却。
/// 注意：该 Buff 需要在 BattleController 的技能释放逻辑中配合检查/标记。
/// </summary>
public class BeginnerBuff : Buff
{
    public override string DisplayName => "初心者";
    public override string Description => "每场战斗第一次技能释放不消耗MP，且该技能不进入冷却";

    public override bool ShowInUI => false;

    private bool hasUsedFirstSkill;

    public bool HasUsedFirstSkill => hasUsedFirstSkill;

    public BeginnerBuff(IBattleUnit owner, bool hasUsedFirstSkill = false)
        : base(owner, -1)
    {
        this.hasUsedFirstSkill = hasUsedFirstSkill;
    }

    public override void OnApplied()
    {
        Debug.Log($"BeginnerBuff: Applied to {Owner?.DisplayName}");
    }

    public bool CanTriggerFreeSkill()
    {
        return !hasUsedFirstSkill;
    }

    public void MarkFirstSkillUsed()
    {
        if (hasUsedFirstSkill)
            return;

        hasUsedFirstSkill = true;
        Debug.Log($"BeginnerBuff: {Owner?.DisplayName} used first free skill");
    }

    public override void OnRemoved()
    {
        Debug.Log($"BeginnerBuff: Removed from {Owner?.DisplayName}");
    }
}
