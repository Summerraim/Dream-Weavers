using UnityEngine;

/// <summary>
/// 防御降低 - 物理防御力降低
/// </summary>
public class WeakenDefenseDebuff : Buff
{
    private readonly int defenseReduction;
    private readonly float defenseReductionPercent;

    public override string DisplayName => "防御降低";
    public override string Description =>
        defenseReductionPercent > 0
            ? $"防御力降低{(defenseReductionPercent * 100):F0}%"
            : $"防御力降低{defenseReduction}点";

    /// <summary>
    /// 基于百分比的防御降低
    /// </summary>
    public WeakenDefenseDebuff(IBattleUnit owner, int duration, float percent)
        : base(owner, duration)
    {
        defenseReduction = 0;
        defenseReductionPercent = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// 基于固定值的防御降低
    /// </summary>
    public WeakenDefenseDebuff(IBattleUnit owner, int duration, int reduction)
        : base(owner, duration)
    {
        defenseReduction = Mathf.Max(0, reduction);
        defenseReductionPercent = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} afflicted with Weaken Defense for {RemainingTurns} turns");
    }

    public override int GetDefenseBonus()
    {
        if (Owner == null)
            return 0;

        int penalty = -defenseReduction;
        if (defenseReductionPercent > 0f)
        {
            penalty -= Mathf.RoundToInt(Owner.Defense * defenseReductionPercent);
        }
        return penalty;
    }
}
