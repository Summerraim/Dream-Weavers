using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蘑菇军团羁绊：协同进化
/// (3) 当场上存在至少3个蘑菇单位时，所有蘑菇单位获得"协同进化"：
///     每有一个蘑菇单位存活，所有蘑菇单位攻击力+5%，防御力+5%
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/MushroomArmy")]
public class MushroomArmy : Synergy
{
    [Header("配置")]
    [SerializeField, Range(0f, 0.5f)]
    private float bonusPerUnit = 0.05f; // 5%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 蘑菇军团只有一个档位(3)
        if (tier < 0)
        {
            RemoveMushroomArmyBuff(model);
            return;
        }

        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 为所有蘑菇单位添加Buff
        var allSpirits = GetAllDeployedSpirits();
        if (allSpirits == null)
            return;

        foreach (var spiritData in allSpirits)
        {
            // 检查Spirit是否还活着
            if (!SacrificeSynergyBridge.IsSpiritAlive(spiritData))
                continue;

            // 检查是否拥有蘑菇军团羁绊
            bool hasMushroom = false;
            if (spiritData.Synergies != null)
            {
                foreach (var synergy in spiritData.Synergies)
                {
                    if (synergy == this)
                    {
                        hasMushroom = true;
                        break;
                    }
                }
            }

            if (hasMushroom)
            {
                // 只有当前在场的Spirit才能应用Buff
                if (
                    battleModel.PlayerUnit != null
                    && battleModel.PlayerUnit.DisplayName == spiritData.DisplayName
                )
                {
                    // 移除旧的Buff
                    RemoveMushroomArmyBuffFromUnit(battleModel.PlayerUnit, battleModel);

                    // 添加新的Buff
                    var mushroomBuff = new MushroomArmyBuff(
                        battleModel.PlayerUnit,
                        bonusPerUnit,
                        battleModel,
                        this
                    );
                    battleModel.AddBuff(mushroomBuff);

                    Debug.Log($"MushroomArmy: Applied to {battleModel.PlayerUnit.DisplayName}");
                }
            }
        }
    }

    private void RemoveMushroomArmyBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        RemoveMushroomArmyBuffFromUnit(model.Owner, battleModel);
    }

    private void RemoveMushroomArmyBuffFromUnit(IBattleUnit unit, BattleModel battleModel)
    {
        var buffs = battleModel.GetBuffsForUnit(unit);
        foreach (var buff in buffs)
        {
            if (buff is MushroomArmyBuff)
            {
                battleModel.RemoveBuff(buff);
                break;
            }
        }
    }

    private BattleModel GetBattleModel()
    {
        return BerserkerSynergyBridge.CurrentBattle;
    }

    private List<SpiritData> GetAllDeployedSpirits()
    {
        return SacrificeSynergyBridge.DeployedSpirits;
    }
}
