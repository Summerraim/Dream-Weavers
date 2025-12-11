using UnityEngine;

/// <summary>
/// 攻击降低 - 物理攻击力降低
/// </summary>
public class WeakenAttackDebuff : Buff
{
    private readonly int damageReduction;
    private readonly float damageReductionPercent;

    public override string DisplayName => "攻击降低";
    public override string Description =>
        damageReductionPercent > 0
            ? $"攻击力降低{(damageReductionPercent * 100):F0}%"
            : $"攻击力降低{damageReduction}点";

    /// <summary>
    /// 基于百分比的攻击降低
    /// </summary>
    public WeakenAttackDebuff(IBattleUnit owner, int duration, float percent)
        : base(owner, duration)
    {
        damageReduction = 0;
        damageReductionPercent = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// 基于固定值的攻击降低
    /// </summary>
    public WeakenAttackDebuff(IBattleUnit owner, int duration, int reduction)
        : base(owner, duration)
    {
        damageReduction = Mathf.Max(0, reduction);
        damageReductionPercent = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} afflicted with Weaken Attack for {RemainingTurns} turns");
    }

    public override int GetDamageBonus()
    {
        if (Owner == null)
            return 0;

        int penalty = -damageReduction;
        if (damageReductionPercent > 0f)
        {
            penalty -= Mathf.RoundToInt(Owner.Damage * damageReductionPercent);
        }
        return penalty;
    }
}
