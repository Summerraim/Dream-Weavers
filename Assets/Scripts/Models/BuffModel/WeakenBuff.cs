using UnityEngine;

/// <summary>
/// 虚弱Debuff：降低攻击力和防御力
/// </summary>
public class WeakenBuff : Buff
{
    public override string DisplayName => "虚弱";
    public override string Description =>
        $"造成伤害降低{damageReduction * 100}%，护甲降低{defenseReduction * 100}%";

    private float damageReduction;  // 伤害降低百分比（0.2 = 20%）
    private float defenseReduction; // 护甲降低百分比（0.2 = 20%）
    private int defenseDebuff;      // 实际降低的护甲值

    public WeakenBuff(IBattleUnit owner, int duration, float damageReduction, float defenseReduction)
        : base(owner, duration)
    {
        this.damageReduction = Mathf.Clamp01(damageReduction);
        this.defenseReduction = Mathf.Clamp01(defenseReduction);
        this.defenseDebuff = 0;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // 计算护甲降低值（基于当前防御力）
        float currentDefense = Owner.Defense;
        defenseDebuff = Mathf.CeilToInt(currentDefense * defenseReduction);

        Debug.Log(
            $"WeakenBuff: Applied to {Owner.DisplayName}, " +
            $"Damage reduction: {damageReduction * 100}%, " +
            $"Defense reduction: -{defenseDebuff} ({defenseReduction * 100}%)"
        );
    }

    /// <summary>
    /// 降低造成的伤害
    /// </summary>
    public override int ModifyDamageDealt(int baseDamage)
    {
        // 降低伤害百分比
        int reducedDamage = Mathf.CeilToInt(baseDamage * (1f - damageReduction));

        Debug.Log(
            $"WeakenBuff: {Owner?.DisplayName} damage reduced from {baseDamage} to {reducedDamage}"
        );

        return reducedDamage;
    }

    /// <summary>
    /// 降低防御力
    /// </summary>
    public override int GetDefenseBonus()
    {
        // 返回负值来降低防御
        return -defenseDebuff;
    }

    public override void OnRemoved()
    {
        Debug.Log($"WeakenBuff: Removed from {Owner?.DisplayName}");
    }
}
