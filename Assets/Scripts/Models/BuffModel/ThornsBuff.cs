using UnityEngine;

/// <summary>
/// 反伤 - 受到攻击时，对攻击者造成固定或比例的反伤
/// </summary>
public class ThornsBuff : Buff
{
    private readonly int flatDamage;
    private readonly float damagePercent;

    public override string DisplayName => "反伤";
    public override string Description => damagePercent > 0
        ? $"反弹受到伤害的{(damagePercent * 100):F0}%"
        : $"反弹{flatDamage}点固定伤害";

    /// <summary>
    /// 基于百分比的反伤
    /// </summary>
    public ThornsBuff(IBattleUnit owner, int duration, float percent, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatDamage = 0;
        damagePercent = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// 基于固定值的反伤
    /// </summary>
    public ThornsBuff(IBattleUnit owner, int duration, int fixedDamage, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatDamage = Mathf.Max(0, fixedDamage);
        damagePercent = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Thorns for {RemainingTurns} turns");
    }

    public override void OnDamageReceived(int actualDamage, IBattleUnit attacker)
    {
        base.OnDamageReceived(actualDamage, attacker);

        if (attacker == null || actualDamage <= 0)
            return;

        int reflectDamage = flatDamage;
        if (damagePercent > 0f)
        {
            reflectDamage += Mathf.RoundToInt(actualDamage * damagePercent);
        }

        if (reflectDamage > 0)
        {
            attacker.ReceiveDamage(reflectDamage);
            Debug.Log($"{Owner?.DisplayName}'s Thorns dealt {reflectDamage} damage to {attacker.DisplayName}");
        }
    }
}
