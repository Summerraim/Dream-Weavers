using UnityEngine;

/// <summary>
/// 复活 - 死亡后立即复活并恢复一定比例生命值（一次性）
/// </summary>
public class ReviveBuff : Buff
{
    private readonly float reviveHealthPercent;

    public override string DisplayName => "复活";
    public override string Description =>
        $"死亡时复活并恢复{(reviveHealthPercent * 100):F0}%生命值";

    public ReviveBuff(IBattleUnit owner, float healthPercent)
        : base(owner, 999) // 持续到触发为止
    {
        reviveHealthPercent = Mathf.Clamp01(healthPercent);
        IsOneTime = true;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Revive buff");
    }

    public override bool OnDeath()
    {
        if (HasTriggered)
            return false;

        HasTriggered = true;

        if (Owner == null)
            return false;

        int reviveHealth = Mathf.CeilToInt(Owner.MaxHP * reviveHealthPercent);
        if (reviveHealth > 0)
        {
            Owner.ReceiveHeal(reviveHealth);
            Debug.Log($"{Owner.DisplayName} revived with {reviveHealth} HP!");
            return true; // 阻止死亡
        }

        return false;
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        if (HasTriggered)
        {
            Debug.Log($"{Owner?.DisplayName}'s Revive buff was consumed");
        }
    }
}
