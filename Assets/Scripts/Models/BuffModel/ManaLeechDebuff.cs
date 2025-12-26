using UnityEngine;

/// <summary>
/// 能量流失 - 每回合损失魔法值
/// </summary>
public class ManaLeechDebuff : Buff
{
    private readonly int flatManaLoss;
    private readonly float percentManaLoss;

    public override string DisplayName => "能量流失";
    public override string Description => percentManaLoss > 0
        ? $"每回合损失{(percentManaLoss * 100):F0}%最大魔法值"
        : $"每回合损失{flatManaLoss}点魔法值";

    /// <summary>
    /// 基于百分比的魔法流失
    /// </summary>
    public ManaLeechDebuff(IBattleUnit owner, int duration, float percent, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatManaLoss = 0;
        percentManaLoss = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// 基于固定值的魔法流失
    /// </summary>
    public ManaLeechDebuff(IBattleUnit owner, int duration, int manaLoss, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        flatManaLoss = Mathf.Max(0, manaLoss);
        percentManaLoss = 0f;
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} afflicted with Mana Leech for {RemainingTurns} turns");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (Owner == null)
            return;

        int manaLoss = flatManaLoss;
        if (percentManaLoss > 0f && Owner.MaxMana > 0)
        {
            manaLoss += Mathf.CeilToInt(Owner.MaxMana * percentManaLoss);
        }

        if (manaLoss > 0)
        {
            if (Owner is Spirit spirit)
            {
                spirit.ConsumeMana(manaLoss);
                Debug.Log($"{Owner.DisplayName} lost {manaLoss} mana from Mana Leech");
            }
            else if (Owner is Enemy enemy)
            {
                enemy.ConsumeMana(manaLoss);
                Debug.Log($"{Owner.DisplayName} lost {manaLoss} mana from Mana Leech");
            }
        }
    }
}
