using UnityEngine;

/// <summary>
/// 学者最大法力值Buff：增加最大法力值百分比
/// </summary>
public class ScholarBuff : Buff
{
    public override string DisplayName => "学者";
    public override string Description => $"最大法力值+{manaBonus * 100}%";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float manaBonus;
    private int addedMana; // 记录增加的法力值

    public ScholarBuff(IBattleUnit owner, float manaBonus)
        : base(owner, -1) // 永久Buff
    {
        this.manaBonus = manaBonus;
        this.addedMana = 0;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // SetMaxManaBonusPercent是Spirit类的方法，需要转换类型
        var spirit = Owner as Spirit;
        if (spirit == null)
        {
            Debug.LogWarning($"ScholarBuff: Owner {Owner.DisplayName} is not a Spirit, cannot apply bonus");
            return;
        }

        // 计算并增加最大法力值
        int oldMaxMana = Owner.MaxMana;
        spirit.SetMaxManaBonusPercent(manaBonus);
        addedMana = Owner.MaxMana - oldMaxMana;

        // 同时恢复增加的法力值
        spirit.ReceiveMana(addedMana);

        Debug.Log(
            $"ScholarBuff: Applied to {Owner.DisplayName}, MaxMana: {oldMaxMana} -> {Owner.MaxMana} (+{addedMana})"
        );
    }

    public override void OnRemoved()
    {
        Debug.Log($"ScholarBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
