using UnityEngine;

/// <summary>
/// 冰川纪元Buff：增加防御力，攻击时有几率冰冻敌人
/// </summary>
public class GlacialEpochBuff : Buff
{
    public override string DisplayName => "冰川纪元";
    public override string Description =>
        $"防御力+{defenseBonus * 100}%，攻击时有{freezeChance * 100}%几率冻结敌人1回合";

    // 不在UI中显示此Buff
    public override bool ShowInUI => false;

    private float defenseBonus;
    private float freezeChance;
    private int bonusDefense;
    private BattleModel battleModel;

    public GlacialEpochBuff(
        IBattleUnit owner,
        float defenseBonus,
        float freezeChance,
        BattleModel battleModel
    )
        : base(owner, -1) // 永久Buff
    {
        this.defenseBonus = defenseBonus;
        this.freezeChance = freezeChance;
        this.battleModel = battleModel;
        this.bonusDefense = 0;
    }

    public override void OnApplied()
    {
        if (Owner == null)
            return;

        // 计算防御力加成
        int baseDefense = (Owner as Spirit)?.BaseDefense ?? 0;
        bonusDefense = Mathf.CeilToInt(baseDefense * defenseBonus);

        Debug.Log(
            $"GlacialEpochBuff: Applied to {Owner.DisplayName}, bonus defense: {bonusDefense} ({defenseBonus * 100}%), freeze chance: {freezeChance * 100}%"
        );
    }

    public override int GetDefenseBonus()
    {
        return bonusDefense;
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        if (Owner == null || target == null || actualDamage <= 0 || battleModel == null)
            return;

        // 根据几率冰冻敌人
        float roll = Random.value;
        if (roll < freezeChance)
        {
            var frozenDebuff = new FrozenDebuff(target, 1); // 冰冻1回合
            battleModel.AddBuff(frozenDebuff);

            Debug.Log(
                $"GlacialEpochBuff: {Owner.DisplayName} froze {target.DisplayName} (roll: {roll:F2} < {freezeChance:F2})"
            );
        }
    }

    public override void OnRemoved()
    {
        Debug.Log($"GlacialEpochBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
