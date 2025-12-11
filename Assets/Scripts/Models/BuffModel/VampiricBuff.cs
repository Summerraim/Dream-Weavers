using UnityEngine;

/// <summary>
/// 吸血 - 攻击造成的伤害一定比例转化为自身生命值
/// </summary>
public class VampiricBuff : Buff
{
    private readonly float lifeStealPercent;

    public override string DisplayName => "吸血";
    public override string Description => $"造成伤害的{(lifeStealPercent * 100):F0}%转化为生命值";

    public VampiricBuff(IBattleUnit owner, int duration, float percent)
        : base(owner, duration)
    {
        lifeStealPercent = Mathf.Clamp01(percent);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log(
            $"{Owner?.DisplayName} gained Vampiric: {(lifeStealPercent * 100):F0}% life steal for {RemainingTurns} turns"
        );
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        base.OnDamageDealt(actualDamage, target);

        if (Owner == null || actualDamage <= 0)
            return;

        int healAmount = Mathf.CeilToInt(actualDamage * lifeStealPercent);
        if (healAmount > 0)
        {
            Owner.ReceiveHeal(healAmount);
            Debug.Log(
                $"{Owner.DisplayName} healed {healAmount} HP from Vampiric (dealt {actualDamage} damage)"
            );
        }
    }
}
