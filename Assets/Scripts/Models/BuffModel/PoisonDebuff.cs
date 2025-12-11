using UnityEngine;

/// <summary>
/// 中毒 - 每回合损失一定生命值
/// </summary>
public class PoisonDebuff : Buff
{
    private readonly int damagePerTurn;
    private readonly float damagePercent;

    public override string DisplayName => "中毒";
    public override string Description => damagePercent > 0
        ? $"每回合损失{(damagePercent * 100):F0}%最大生命值"
        : $"每回合损失{damagePerTurn}点生命值";

    /// <summary>
    /// 基于百分比的中毒伤害
    /// </summary>
    public PoisonDebuff(IBattleUnit owner, int duration, float percent, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        damagePerTurn = 0;
        damagePercent = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// 基于固定值的中毒伤害
    /// </summary>
    public PoisonDebuff(IBattleUnit owner, int duration, int damage, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        damagePerTurn = Mathf.Max(0, damage);
        damagePercent = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} is poisoned for {RemainingTurns} turns");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (Owner == null)
            return;

        int damage = damagePerTurn;
        if (damagePercent > 0f && Owner.MaxHP > 0)
        {
            damage += Mathf.CeilToInt(Owner.MaxHP * damagePercent);
        }

        if (damage > 0)
        {
            Owner.ReceiveDamage(damage);
            Debug.Log($"{Owner.DisplayName} took {damage} poison damage");
        }
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        Debug.Log($"{Owner?.DisplayName} is no longer poisoned");
    }
}
