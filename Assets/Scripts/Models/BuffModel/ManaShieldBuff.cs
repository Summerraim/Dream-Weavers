using UnityEngine;

/// <summary>
/// 能量护甲 - 将一部分攻击伤害转化为魔法消耗
/// </summary>
public class ManaShieldBuff : Buff
{
    private readonly float damageToManaRatio; // 伤害转魔法的比例
    private readonly float absorptionPercent; // 吸收的伤害百分比

    public override string DisplayName => "能量护甲";
    public override string Description => $"吸收{(absorptionPercent * 100):F0}%伤害转化为魔法消耗";

    /// <summary>
    /// 创建能量护甲Buff
    /// </summary>
    /// <param name="owner">拥有者</param>
    /// <param name="duration">持续回合</param>
    /// <param name="absorption">吸收的伤害百分比</param>
    /// <param name="ratio">伤害转魔法的比例（1.0表示1点伤害=1点魔法）</param>
    public ManaShieldBuff(IBattleUnit owner, int duration, float absorption, float ratio = 1.0f)
        : base(owner, duration)
    {
        absorptionPercent = Mathf.Clamp01(absorption);
        damageToManaRatio = Mathf.Max(0f, ratio);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Mana Shield for {RemainingTurns} turns");
    }

    public override int ModifyDamageReceived(int baseDamage)
    {
        if (Owner == null || baseDamage <= 0)
            return baseDamage;

        // 计算要吸收的伤害
        int absorbedDamage = Mathf.RoundToInt(baseDamage * absorptionPercent);
        if (absorbedDamage <= 0)
            return baseDamage;

        // 计算需要的魔法值
        int requiredMana = Mathf.CeilToInt(absorbedDamage * damageToManaRatio);
        int availableMana = Owner.Mana;

        if (availableMana >= requiredMana)
        {
            // 魔法值足够，完全吸收
            if (Owner is Spirit spirit)
            {
                spirit.ConsumeMana(requiredMana);
            }
            else if (Owner is Enemy enemy)
            {
                enemy.ConsumeMana(requiredMana);
            }

            int finalDamage = baseDamage - absorbedDamage;
            Debug.Log(
                $"{Owner.DisplayName}'s Mana Shield absorbed {absorbedDamage} damage (cost {requiredMana} mana)"
            );
            return Mathf.Max(0, finalDamage);
        }
        else if (availableMana > 0)
        {
            // 魔法值不足，部分吸收
            int partialAbsorb = Mathf.FloorToInt(availableMana / damageToManaRatio);

            if (Owner is Spirit spirit)
            {
                spirit.ConsumeMana(availableMana);
            }
            else if (Owner is Enemy enemy)
            {
                enemy.ConsumeMana(availableMana);
            }

            int finalDamage = baseDamage - partialAbsorb;
            Debug.Log(
                $"{Owner.DisplayName}'s Mana Shield partially absorbed {partialAbsorb} damage (used all {availableMana} mana)"
            );
            return Mathf.Max(0, finalDamage);
        }

        // 没有魔法值，无法吸收
        return baseDamage;
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        Debug.Log($"{Owner?.DisplayName}'s Mana Shield expired");
    }
}
