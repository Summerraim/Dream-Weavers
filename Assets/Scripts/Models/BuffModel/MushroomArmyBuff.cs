using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蘑菇军团Buff：协同进化，每有一个蘑菇单位存活，所有蘑菇单位攻击力和防御力提升
/// </summary>
public class MushroomArmyBuff : Buff
{
    public override string DisplayName => "蘑菇军团·协同进化";
    public override string Description =>
        $"每个蘑菇单位存活时，所有蘑菇单位攻击力+{bonusPerUnit * 100}%，防御力+{bonusPerUnit * 100}%";

    private float bonusPerUnit;
    private BattleModel battleModel;
    private Synergy mushroomSynergy;

    public MushroomArmyBuff(
        IBattleUnit owner,
        float bonusPerUnit,
        BattleModel battleModel,
        Synergy mushroomSynergy
    )
        : base(owner, -1) // 永久Buff
    {
        this.bonusPerUnit = bonusPerUnit;
        this.battleModel = battleModel;
        this.mushroomSynergy = mushroomSynergy;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"MushroomArmyBuff: Applied to {Owner?.DisplayName}, bonus per unit: {bonusPerUnit * 100}%"
        );
    }

    public override int GetDamageBonus()
    {
        int mushroomCount = CountAliveMushroomUnits();
        if (mushroomCount <= 0 || Owner == null)
            return 0;

        int baseDamage = (Owner as Spirit)?.BaseDamage ?? 0;
        int bonus = Mathf.CeilToInt(baseDamage * bonusPerUnit * mushroomCount);
        return bonus;
    }

    public override int GetDefenseBonus()
    {
        int mushroomCount = CountAliveMushroomUnits();
        if (mushroomCount <= 0 || Owner == null)
            return 0;

        int baseDefense = (Owner as Spirit)?.BaseDefense ?? 0;
        int bonus = Mathf.CeilToInt(baseDefense * bonusPerUnit * mushroomCount);
        return bonus;
    }

    private int CountAliveMushroomUnits()
    {
        if (battleModel == null || mushroomSynergy == null)
            return 0;

        var allSpirits = SacrificeSynergyBridge.DeployedSpirits;
        if (allSpirits == null)
            return 0;

        int count = 0;
        foreach (var spiritData in allSpirits)
        {
            // 检查Spirit是否还活着
            if (!SacrificeSynergyBridge.IsSpiritAlive(spiritData))
                continue;

            // 检查是否拥有蘑菇军团羁绊
            if (spiritData.Synergies != null)
            {
                foreach (var synergy in spiritData.Synergies)
                {
                    if (synergy == mushroomSynergy)
                    {
                        count++;
                        break;
                    }
                }
            }
        }

        return count;
    }

    public override void OnRemoved()
    {
        Debug.Log($"MushroomArmyBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
