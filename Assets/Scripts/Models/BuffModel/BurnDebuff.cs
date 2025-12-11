using UnityEngine;

/// <summary>
/// 燃烧Debuff - 每回合造成火焰伤害
/// </summary>
public class BurnDebuff : Buff
{
    public override string DisplayName => "燃烧";
    public override string Description => "被火焰灼烧，持续受到伤害";

    private int damagePerTurn;
    private float damagePercent;
    private bool usePercent;

    public BurnDebuff(
        IBattleUnit owner,
        int duration,
        int flatDamage,
        float percentDamage,
        bool usePercentDamage
    )
        : base(owner, duration)
    {
        damagePerTurn = flatDamage;
        damagePercent = percentDamage;
        usePercent = usePercentDamage;
    }

    public override void OnApplied()
    {
        string damageInfo = usePercent ? $"{damagePercent * 100}%最大生命值" : $"{damagePerTurn}点";

        Debug.Log(
            $"<color=orange>{Owner?.DisplayName} 被点燃了！每回合受到 {damageInfo} 火焰伤害，持续 {RemainingTurns} 回合</color>"
        );
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (Owner == null || Owner.IsDead)
            return;

        int damage = damagePerTurn;

        if (usePercent && Owner.MaxHP > 0)
        {
            damage = Mathf.CeilToInt(Owner.MaxHP * damagePercent);
        }

        if (damage > 0)
        {
            Owner.ReceiveDamage(damage);
            Debug.Log($"<color=orange>🔥 {Owner.DisplayName} 受到 {damage} 点燃烧伤害！</color>");
        }
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 身上的火焰熄灭了");
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍在燃烧，剩余 {RemainingTurns} 回合");
        }
    }
}
