using UnityEngine;

/// <summary>
/// 最大生命值加成Buff：增加最大生命值百分比
/// </summary>
public class MaxHealthBuff : Buff
{
    public override string DisplayName => "生命强化";
    public override string Description => $"最大生命值+{healthBonus * 100}%";

    private float healthBonus;
    private int addedHealth; // 记录增加的生命值

    public MaxHealthBuff(IBattleUnit owner, float healthBonus)
        : base(owner, -1) // 永久Buff
    {
        this.healthBonus = healthBonus;
        this.addedHealth = 0;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // SetMaxHpBonusPercent是Spirit类的方法，需要转换类型
        var spirit = Owner as Spirit;
        if (spirit == null)
        {
            Debug.LogWarning($"MaxHealthBuff: Owner {Owner.DisplayName} is not a Spirit, cannot apply bonus");
            return;
        }

        // 计算并增加最大生命值
        int oldMaxHP = Owner.MaxHP;
        spirit.SetMaxHpBonusPercent(healthBonus);
        addedHealth = Owner.MaxHP - oldMaxHP;

        // 同时恢复增加的生命值
        Owner.ReceiveHeal(addedHealth);

        Debug.Log(
            $"MaxHealthBuff: Applied to {Owner.DisplayName}, MaxHP: {oldMaxHP} -> {Owner.MaxHP} (+{addedHealth})"
        );
    }

    public override void OnRemoved()
    {
        // 如果需要移除效果，可以在这里减少生命值
        Debug.Log($"MaxHealthBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
