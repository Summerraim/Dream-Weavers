using UnityEngine;

/// <summary>
/// 诅咒Debuff - 降低最大生命值上限
/// </summary>
public class CurseDebuff : Buff
{
    public override string DisplayName => "诅咒";
    public override string Description => "最大生命值降低";

    private float maxHPReduction;
    private int originalMaxHP;
    private int reducedAmount;

    public CurseDebuff(IBattleUnit owner, int duration, float reduction, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        maxHPReduction = Mathf.Clamp01(reduction);
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // 记录原始最大生命值
        originalMaxHP = Owner.MaxHP;
        reducedAmount = Mathf.RoundToInt(originalMaxHP * maxHPReduction);

        Debug.Log(
            $"{Owner.DisplayName} 被诅咒了！最大生命值降低 {reducedAmount} 点（{maxHPReduction * 100}%），持续 {RemainingTurns} 回合"
        );

        // 如果当前生命值超过新的最大值，调整为新最大值
        if (Owner.HP > originalMaxHP - reducedAmount)
        {
            int overflow = Owner.HP - (originalMaxHP - reducedAmount);
            Owner.ReceiveDamage(overflow);
            Debug.Log($"{Owner.DisplayName} 的当前生命值超过新上限，损失了 {overflow} 点生命");
        }
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 从诅咒中解脱了，最大生命值恢复");
    }

    public int GetMaxHPReduction()
    {
        return reducedAmount;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍被诅咒，剩余 {RemainingTurns} 回合");
        }
    }
}
