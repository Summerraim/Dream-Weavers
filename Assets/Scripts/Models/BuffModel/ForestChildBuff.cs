using UnityEngine;

/// <summary>
/// 森林之子Buff：增加最大生命值和法力值百分比，6档额外获得生命偷取
/// </summary>
public class ForestChildBuff : Buff
{
    public override string DisplayName => "森林之子";
    public override string Description
    {
        get
        {
            string desc = $"最大生命值+{hpBonus * 100}%，最大法力值+{manaBonus * 100}%";
            if (lifeStealPercent > 0)
            {
                desc += $"，生命偷取+{lifeStealPercent * 100}%";
            }
            return desc;
        }
    }

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float hpBonus;
    private float manaBonus;
    private float lifeStealPercent;

    public ForestChildBuff(
        IBattleUnit owner,
        float hpBonus,
        float manaBonus,
        float lifeStealPercent = 0f
    )
        : base(owner, -1) // 永久Buff
    {
        this.hpBonus = hpBonus;
        this.manaBonus = manaBonus;
        this.lifeStealPercent = lifeStealPercent;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        var spirit = Owner as Spirit;
        if (spirit != null)
        {
            int oldMaxHP = spirit.MaxHP;
            int oldMaxMana = spirit.MaxMana;

            // 设置最大生命值和法力值加成
            spirit.SetMaxHpBonusPercent(spirit.MaxHpBonusPercent + hpBonus);
            spirit.SetMaxManaBonusPercent(spirit.MaxManaBonusPercent + manaBonus);

            Debug.Log(
                $"ForestChildBuff: Applied to {Owner.DisplayName}, HP: {oldMaxHP}->{spirit.MaxHP}, Mana: {oldMaxMana}->{spirit.MaxMana}"
            );
        }
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        // 仅在有生命偷取时触发
        if (lifeStealPercent <= 0 || Owner == null || actualDamage <= 0)
            return;

        int healAmount = Mathf.CeilToInt(actualDamage * lifeStealPercent);
        if (healAmount > 0)
        {
            int oldHP = Owner.HP;
            Owner.ReceiveHeal(healAmount);
            int actualHeal = Owner.HP - oldHP;

            Debug.Log(
                $"ForestChildBuff: {Owner.DisplayName} heals {actualHeal} HP from {actualDamage} damage (life steal)"
            );
        }
    }

    public override void OnRemoved()
    {
        if (Owner == null)
            return;

        var spirit = Owner as Spirit;
        if (spirit != null)
        {
            // 移除加成
            spirit.SetMaxHpBonusPercent(spirit.MaxHpBonusPercent - hpBonus);
            spirit.SetMaxManaBonusPercent(spirit.MaxManaBonusPercent - manaBonus);

            Debug.Log($"ForestChildBuff: Removed from {Owner.DisplayName}");
        }
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
