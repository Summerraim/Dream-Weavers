using UnityEngine;

/// <summary>
/// 暴击强化Buff - 提高攻击力并有几率暴击
/// </summary>
public class CriticalStrikeBuff : Buff
{
    public override string DisplayName => "暴击强化";
    public override string Description => "攻击力提升，并有几率造成暴击";

    private float damageBonus;
    private float critChance;
    private float critMultiplier;

    public CriticalStrikeBuff(
        IBattleUnit owner,
        int duration,
        float bonus,
        float chance,
        float multiplier
    )
        : base(owner, duration)
    {
        damageBonus = Mathf.Max(0f, bonus);
        critChance = Mathf.Clamp01(chance);
        critMultiplier = Mathf.Max(1f, multiplier);
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"{Owner?.DisplayName} 获得了暴击强化，攻击力提升 {damageBonus * 100}%，暴击几率 {critChance * 100}%，持续 {RemainingTurns} 回合"
        );
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 的暴击强化消失了");
    }

    public override int GetDamageBonus()
    {
        if (Owner == null)
            return 0;
        return Mathf.RoundToInt(Owner.Damage * damageBonus);
    }

    public override int ModifyDamageDealt(int baseDamage)
    {
        // 判断是否暴击
        float roll = Random.Range(0f, 1f);
        if (roll < critChance)
        {
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            Debug.Log($"{Owner?.DisplayName} 触发暴击！伤害从 {baseDamage} 提升到 {critDamage}");
            return critDamage;
        }
        return baseDamage;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 的暴击强化剩余 {RemainingTurns} 回合");
        }
    }
}
