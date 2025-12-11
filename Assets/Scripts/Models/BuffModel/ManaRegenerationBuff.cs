using UnityEngine;

/// <summary>
/// 能量充沛 - 每回合恢复法力值
/// </summary>
public class ManaRegenerationBuff : Buff
{
    private readonly int flatRegeneration;
    private readonly float percentRegeneration;

    public override string DisplayName => "能量充沛";
    public override string Description => percentRegeneration > 0
        ? $"每回合恢复{(percentRegeneration * 100):F0}%最大法力值"
        : $"每回合恢复{flatRegeneration}点法力值";

    public ManaRegenerationBuff(IBattleUnit owner, int duration, float percentRegen)
        : base(owner, duration)
    {
        flatRegeneration = 0;
        percentRegeneration = Mathf.Clamp01(percentRegen);
    }

    public ManaRegenerationBuff(IBattleUnit owner, int duration, int flatRegen)
        : base(owner, duration)
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
            // 需要通过某种方式恢复法力值
            // 注意：IBattleUnit 接口没有恢复法力值的方法，需要检查实际类型
            if (Owner is Spirit spirit)
            {
                int currentMana = spirit.Mana;
                int newMana = Mathf.Min(spirit.MaxMana, currentMana + regenAmount);
                // 这里需要一个方法来设置法力值
                Debug.Log($"{Owner.DisplayName} regenerated {regenAmount} mana (实际恢复需要添加方法)");
            }
            else if (Owner is Enemy enemy)
            {
                int currentMana = enemy.Mana;
                int newMana = Mathf.Min(enemy.MaxMana, currentMana + regenAmount);
                Debug.Log($"{Owner.DisplayName} regenerated {regenAmount} mana (实际恢复需要添加方法)");
            }
        }
    }
}
