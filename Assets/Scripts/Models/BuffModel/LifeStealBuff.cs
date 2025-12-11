using UnityEngine;

/// <summary>
/// 生命偷取Buff：攻击时恢复伤害的百分比生命值
/// </summary>
public class LifeStealBuff : Buff
{
    public override string DisplayName => "生命偷取";
    public override string Description => $"攻击时恢复造成伤害的{lifeStealPercent * 100}%生命值";

    private float lifeStealPercent;

    public LifeStealBuff(IBattleUnit owner, float lifeStealPercent)
        : base(owner, -1) // 永久Buff
    {
        this.lifeStealPercent = lifeStealPercent;
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        if (Owner == null || actualDamage <= 0)
            return;

        int healAmount = Mathf.CeilToInt(actualDamage * lifeStealPercent);
        if (healAmount > 0)
        {
            int oldHP = Owner.HP;
            Owner.ReceiveHeal(healAmount);
            int actualHeal = Owner.HP - oldHP;

            Debug.Log(
                $"LifeStealBuff: {Owner.DisplayName} heals {actualHeal} HP from {actualDamage} damage"
            );
        }
    }

    public override void OnApplied()
    {
        Debug.Log($"LifeStealBuff: Applied to {Owner?.DisplayName}, {lifeStealPercent * 100}% life steal");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
