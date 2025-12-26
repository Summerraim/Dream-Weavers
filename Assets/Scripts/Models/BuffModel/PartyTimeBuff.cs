using UnityEngine;

/// <summary>
/// 派对狂欢Buff：释放技能时有几率不进入冷却
/// 注意：此Buff需要在BattleModel的技能释放逻辑中检查
/// 当检测到单位拥有此Buff时，技能释放后有一定几率不设置冷却时间
/// </summary>
public class PartyTimeBuff : Buff
{
    public override string DisplayName => "派对狂欢";
    public override string Description => $"释放技能时有{noCooldownChance * 100}%几率没有冷却回合";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float noCooldownChance;

    public float NoCooldownChance => noCooldownChance;

    public PartyTimeBuff(IBattleUnit owner, float noCooldownChance)
        : base(owner, -1) // 永久Buff
    {
        this.noCooldownChance = noCooldownChance;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"PartyTimeBuff: Applied to {Owner?.DisplayName}, no cooldown chance: {noCooldownChance * 100}%"
        );
    }

    /// <summary>
    /// 检查是否触发无冷却效果
    /// 在BattleModel的技能释放后调用此方法
    /// </summary>
    public bool TryTriggerNoCooldown()
    {
        float roll = Random.value;
        bool triggered = roll < noCooldownChance;

        if (triggered)
        {
            Debug.Log(
                $"PartyTimeBuff: {Owner?.DisplayName} triggered no cooldown! (roll: {roll:F2} < {noCooldownChance:F2})"
            );
        }

        return triggered;
    }

    public override void OnRemoved()
    {
        Debug.Log($"PartyTimeBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
