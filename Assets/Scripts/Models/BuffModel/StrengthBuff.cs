using UnityEngine;

/// <summary>
/// 力量祝福 - 提升攻击力
/// </summary>
public class StrengthBuff : Buff
{
    private readonly int damageBonus;
    private readonly float damageMultiplier;

    public override string DisplayName => "力量祝福";
    public override string Description => $"攻击力提升{(damageMultiplier * 100):F0}%";

    public StrengthBuff(IBattleUnit owner, int duration, float multiplier, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
        damageBonus = 0;
    }

    public StrengthBuff(IBattleUnit owner, int duration, int flatBonus, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        damageMultiplier = 0f;
        damageBonus = Mathf.Max(0, flatBonus);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Strength Buff: +{(damageMultiplier * 100):F0}% damage for {RemainingTurns} turns");
    }

    public override int GetDamageBonus()
    {
        if (Owner == null)
            return 0;

        int bonus = damageBonus;
        if (damageMultiplier > 0f)
        {
            bonus += Mathf.RoundToInt(Owner.Damage * damageMultiplier);
        }
        return bonus;
    }
}
