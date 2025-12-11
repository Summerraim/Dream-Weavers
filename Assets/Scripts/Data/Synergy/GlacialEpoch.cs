using UnityEngine;

/// <summary>
/// 冰川纪元羁绊：增加防御力，攻击时有几率冰冻敌人
/// (2) +20%防御力，5%几率冰冻敌人1回合
/// (4) +40%防御力，10%几率冰冻敌人1回合
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/GlacialEpoch")]
public class GlacialEpoch : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoDefenseBonus = 0.2f; // 20%

    [SerializeField, Range(0f, 1f)]
    private float tierTwoFreezeChance = 0.05f; // 5%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourDefenseBonus = 0.4f; // 40%

    [SerializeField, Range(0f, 1f)]
    private float tierFourFreezeChance = 0.1f; // 10%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveGlacialEpochBuff(model);
            return;
        }

        float defenseBonus = tier == 0 ? tierTwoDefenseBonus : tierFourDefenseBonus;
        float freezeChance = tier == 0 ? tierTwoFreezeChance : tierFourFreezeChance;

        // 移除旧的Buff
        RemoveGlacialEpochBuff(model);

        // 创建新的Buff
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var glacialBuff = new GlacialEpochBuff(model.Owner, defenseBonus, freezeChance, battleModel);
        battleModel.AddBuff(glacialBuff);

        Debug.Log(
            $"GlacialEpoch: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, Defense+{defenseBonus * 100}%, FreezeChance={freezeChance * 100}%"
        );
    }

    private void RemoveGlacialEpochBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is GlacialEpochBuff)
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
}
