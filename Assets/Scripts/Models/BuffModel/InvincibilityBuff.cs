using UnityEngine;

/// <summary>
/// 无敌 - 一定回合内不受任何伤害
/// </summary>
public class InvincibilityBuff : Buff
{
    public override string DisplayName => "无敌";
    public override string Description => "不受任何伤害";

    public InvincibilityBuff(IBattleUnit owner, int duration)
        : base(owner, duration)
    {
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Invincibility for {RemainingTurns} turns");
    }

    public override int ModifyDamageReceived(int baseDamage)
    {
        // 无敌状态下不受任何伤害
        if (baseDamage > 0)
        {
            Debug.Log($"{Owner?.DisplayName} is invincible! Blocked {baseDamage} damage");
        }
        return 0;
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        Debug.Log($"{Owner?.DisplayName}'s Invincibility ended");
    }
}
