using UnityEngine;

/// <summary>
/// 能量充沛 - 每回合恢复法力值
/// </summary>
public class ManaRegenerationBuff : Buff
{
    private readonly int flatRegeneration;
    private readonly float percentRegeneration;

    public override string DisplayName => "能量充沛";
    public override string Description =>
        percentRegeneration > 0
            ? $"每回合恢复{(percentRegeneration * 100):F0}%最大法力值"
            : $"每回合恢复{flatRegeneration}点法力值";

    public ManaRegenerationBuff(IBattleUnit owner, int duration, float percentRegen, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatRegeneration = 0;
        percentRegeneration = Mathf.Clamp01(percentRegen);
    }

    public ManaRegenerationBuff(IBattleUnit owner, int duration, int flatRegen, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatRegeneration = Mathf.Max(0, flatRegen);
        percentRegeneration = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Mana Regeneration for {RemainingTurns} turns");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (Owner == null)
            return;

        int regenAmount = flatRegeneration;
        if (percentRegeneration > 0f && Owner.MaxMana > 0)
        {
            regenAmount += Mathf.CeilToInt(Owner.MaxMana * percentRegeneration);
        }

        if (regenAmount > 0)
        {
            // 恢复法力值
            if (Owner is Spirit spirit)
            {
                spirit.ReceiveMana(regenAmount);
                Debug.Log(
                    $"{Owner.DisplayName} regenerated {regenAmount} mana ({spirit.Mana}/{spirit.MaxMana})"
                );
            }
            else if (Owner is Enemy enemy)
            {
                enemy.ReceiveMana(regenAmount);
                Debug.Log(
                    $"{Owner.DisplayName} regenerated {regenAmount} mana ({enemy.Mana}/{enemy.MaxMana})"
                );
            }
        }
    }
}
