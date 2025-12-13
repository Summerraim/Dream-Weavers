using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 狩猎大师羁绊：诱捕
/// (3) 当场上存在至少3个狩猎大师单位时，登场获得"诱捕"效果：
///     可在敌方生命值/法力值 ≤ 10% 时捕捉成功
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/HunterMaster")]
public class HunterMaster : Synergy
{
    [Header("配置")]
    [SerializeField, Range(0f, 0.5f)]
    private float captureThreshold = 0.1f; // 10%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 狩猎大师只有一个档位(3)
        if (tier < 0)
        {
            RemoveHunterMasterBuff(model);
            return;
        }

        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 为所有狩猎大师单位添加Buff
        var allSpirits = GetAllDeployedSpirits();
        if (allSpirits == null)
            return;

        foreach (var spiritData in allSpirits)
        {
            // 检查Spirit是否还活着
            if (!SacrificeSynergyBridge.IsSpiritAlive(spiritData))
                continue;

            // 检查是否拥有狩猎大师羁绊
            bool hasHunterMaster = false;
            if (spiritData.Synergies != null)
            {
                foreach (var synergy in spiritData.Synergies)
                {
                    if (synergy == this)
                    {
                        hasHunterMaster = true;
                        break;
                    }
                }
            }

            if (hasHunterMaster)
            {
                // 只有当前在场的Spirit才能应用Buff
                if (
                    battleModel.PlayerUnit != null
                    && battleModel.PlayerUnit.DisplayName == spiritData.DisplayName
                )
                {
                    // 移除旧的Buff
                    RemoveHunterMasterBuffFromUnit(battleModel.PlayerUnit, battleModel);

                    // 添加新的Buff
                    var trapBuff = new HunterMasterBuff(
                        battleModel.PlayerUnit,
                        captureThreshold,
                        battleModel,
                        this
                    );
                    battleModel.AddBuff(trapBuff);

                    Debug.Log($"HunterMaster: Applied Trap buff to {battleModel.PlayerUnit.DisplayName}");
                }
            }
        }
    }

    private void RemoveHunterMasterBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        RemoveHunterMasterBuffFromUnit(model.Owner, battleModel);
    }

    private void RemoveHunterMasterBuffFromUnit(IBattleUnit unit, BattleModel battleModel)
    {
        var buffs = battleModel.GetBuffsForUnit(unit);
        foreach (var buff in buffs)
        {
            if (buff is HunterMasterBuff)
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
