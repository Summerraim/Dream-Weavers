using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 疗愈者羁绊：释放技能时治疗队友
/// (2) 每次释放技能回复队伍里面随机心兽5%最大生命值
/// (4) 回复10%最大生命值
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Healer")]
public class Healer : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoHealPercent = 0.05f; // 5%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourHealPercent = 0.1f; // 10%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveHealerBuff(model);
            return;
        }

        float healPercent = tier == 0 ? tierTwoHealPercent : tierFourHealPercent;

        // 移除旧的Buff
        RemoveHealerBuff(model);

        // 获取所有上场的Spirit
        List<SpiritData> allSpirits = GetAllDeployedSpirits();
        if (allSpirits == null || allSpirits.Count == 0)
        {
            Debug.LogWarning("Healer: Cannot get deployed spirits");
            return;
        }

        // 创建新的Buff
        var healerBuff = new HealerBuff(model.Owner, healPercent, allSpirits);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(healerBuff);
            Debug.Log(
                $"Healer: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, HealPercent={healPercent * 100}%"
            );
        }
    }

    private void RemoveHealerBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is HealerBuff)
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
