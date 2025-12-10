using UnityEngine;

/// <summary>
/// 射手攻击力Buff：永久增加攻击力百分比
/// </summary>
public class ArcherBuff : Buff
{
    public override string DisplayName => "射手";
    public override string Description => $"攻击力+{attackBonus * 100}%";

    private float attackBonus;
    private int bonusDamage;

    public ArcherBuff(IBattleUnit owner, float attackBonus)
        : base(owner, -1) // 永久Buff
    {
        this.attackBonus = attackBonus;
        this.bonusDamage = 0;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // 计算攻击力加成
        int baseDamage = (Owner as Spirit)?.BaseDamage ?? 0;
        bonusDamage = Mathf.CeilToInt(baseDamage * attackBonus);

        Debug.Log(
            $"ArcherBuff: Applied to {Owner.DisplayName}, bonus damage: {bonusDamage} ({attackBonus * 100}%)"
        );
    }

    public override int GetDamageBonus()
    {
        return bonusDamage;
    }

    public override void OnRemoved()
    {
        Debug.Log($"ArcherBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
