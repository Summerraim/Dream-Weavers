using UnityEngine;

/// <summary>
/// 防御提升 - 按百分比提高防御力
/// </summary>
public class DefenseUpBuff : Buff
{
    private readonly float defensePercent;

    public override string DisplayName => "防御提升";
    public override string Description => $"防御力提高{(defensePercent * 100):F0}%";

    public DefenseUpBuff(IBattleUnit owner, int duration, float percent)
        : base(owner, duration)
    {
        defensePercent = Mathf.Clamp01(percent);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Defense Up: {(defensePercent * 100):F0}% for {RemainingTurns} turns");
    }

    public override int GetDefenseBonus()
    {
        if (Owner == null)
            return 0;
        // 将百分比转换为对当前防御的加成值
        int bonus = Mathf.RoundToInt(Owner.Defense * defensePercent);
        return Mathf.Max(0, bonus);
    }
}
