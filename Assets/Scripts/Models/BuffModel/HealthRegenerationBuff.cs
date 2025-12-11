using UnityEngine;

/// <summary>
/// 生命源泉 - 每回合恢复生命值
/// </summary>
public class HealthRegenerationBuff : Buff
{
    private readonly int flatRegeneration;
    private readonly float percentRegeneration;

    public override string DisplayName => "生命源泉";
    public override string Description => percentRegeneration > 0
        ? $"每回合恢复{(percentRegeneration * 100):F0}%最大生命值"
        : $"每回合恢复{flatRegeneration}点生命值";

    public HealthRegenerationBuff(IBattleUnit owner, int duration, float percentRegen)
        : base(owner, duration)
    {
        flatRegeneration = 0;
        percentRegeneration = Mathf.Clamp01(percentRegen);
    }

    public HealthRegenerationBuff(IBattleUnit owner, int duration, int flatRegen)
        : base(owner, duration)
    {
        flatRegeneration = Mathf.Max(0, flatRegen);
        percentRegeneration = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Health Regeneration for {RemainingTurns} turns");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (Owner == null)
            return;

        int regenAmount = flatRegeneration;
        if (percentRegeneration > 0f && Owner.MaxHP > 0)
        {
            regenAmount += Mathf.CeilToInt(Owner.MaxHP * percentRegeneration);
        }

        if (regenAmount > 0)
        {
            Owner.ReceiveHeal(regenAmount);
            Debug.Log($"{Owner.DisplayName} regenerated {regenAmount} HP");
        }
    }
}
