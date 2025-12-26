using UnityEngine;

/// <summary>
/// 全能化身Buff：每次死亡时回复生命值和法力值，可触发多次
/// </summary>
public class OmnipotentBuff : Buff
{
    public override string DisplayName => "全能化身";
    public override string Description =>
        $"每次死亡时回复{revivePercent * 100}%最大生命值和法力值（剩余{remainingRevives}次）";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float revivePercent;
    private int maxRevives;
    private int remainingRevives;

    public int RemainingRevives => remainingRevives;

    public OmnipotentBuff(IBattleUnit owner, float revivePercent, int maxRevives)
        : base(owner, -1) // 永久Buff
    {
        this.revivePercent = revivePercent;
        this.maxRevives = maxRevives;
        this.remainingRevives = maxRevives;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"OmnipotentBuff: Applied to {Owner?.DisplayName}, {revivePercent * 100}% revive, {maxRevives} times"
        );
    }

    public override bool OnDeath()
    {
        if (Owner == null || remainingRevives <= 0)
            return false;

        remainingRevives--;

        // 计算回复量
        var spirit = Owner as Spirit;
        if (spirit != null)
        {
            int healHP = Mathf.CeilToInt(spirit.MaxHP * revivePercent);
            int healMana = Mathf.CeilToInt(spirit.MaxMana * revivePercent);

            // 回复生命值和法力值
            Owner.ReceiveHeal(healHP);

            // 注意：这里需要一个方法来回复法力值
            // 假设Spirit有一个RestoreMana方法
            // spirit.RestoreMana(healMana);

            Debug.Log(
                $"OmnipotentBuff: {Owner.DisplayName} revived with {healHP} HP and {healMana} Mana! Remaining revives: {remainingRevives}"
            );

            return true; // 阻止死亡
        }

        return false;
    }

    public override void OnRemoved()
    {
        Debug.Log($"OmnipotentBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
