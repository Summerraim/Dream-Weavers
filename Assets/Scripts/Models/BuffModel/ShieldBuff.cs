using UnityEngine;

/// <summary>
/// 护盾Buff - 吸收伤害
/// </summary>
public class ShieldBuff : Buff
{
    public override string DisplayName => "护盾";
    public override string Description => "吸收一定伤害";

    private int shieldAmount;
    private int maxShieldAmount; // 记录初始最大护盾值

    public ShieldBuff(IBattleUnit owner, int duration, int shield, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        shieldAmount = Mathf.Max(0, shield);
        maxShieldAmount = shieldAmount;
    }

    /// <summary>
    /// 获取当前护盾值
    /// </summary>
    public int GetCurrentShield() => shieldAmount;

    /// <summary>
    /// 获取最大护盾值
    /// </summary>
    public int GetMaxShield() => maxShieldAmount;

    public override void OnApplied()
    {
        Debug.Log($"{Owner?.DisplayName} 获得了 {shieldAmount} 点护盾，持续 {RemainingTurns} 回合");
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 的护盾消失了");
    }

    public override int ModifyDamageReceived(int baseDamage)
    {
        if (shieldAmount <= 0)
            return baseDamage;

        int absorbed = Mathf.Min(shieldAmount, baseDamage);
        shieldAmount -= absorbed;
        int actualDamage = baseDamage - absorbed;

        Debug.Log($"{Owner?.DisplayName} 的护盾吸收了 {absorbed} 点伤害，剩余护盾 {shieldAmount}");

        if (shieldAmount <= 0)
        {
            Debug.Log($"{Owner?.DisplayName} 的护盾被击破了！");
        }

        return actualDamage;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log(
                $"{Owner?.DisplayName} 的护盾剩余 {shieldAmount} 点，持续 {RemainingTurns} 回合"
            );
        }
    }
}
