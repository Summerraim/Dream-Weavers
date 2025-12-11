using UnityEngine;

/// <summary>
/// 燃法者羁绊：造成伤害时燃烧敌方法力值
/// (2) 造成伤害时额外扣除对方该次伤害10%的最大法力值
/// (4) 扣除比例提升至20%
///
/// 注意：使用默认的Trigger Counts [2, 4, 6]即可，但只有2和4档位有效
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/ManaBurner")]
public class ManaBurner : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoBurnPercent = 0.1f; // 10%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourBurnPercent = 0.2f; // 20%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveManaBurnBuff(model);
            return;
        }

        float burnPercent = tier == 0 ? tierTwoBurnPercent : tierFourBurnPercent;

        // 移除旧的Buff
        RemoveManaBurnBuff(model);

        // 创建新的Buff
        var manaBurnBuff = new ManaBurnBuff(model.Owner, burnPercent);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(manaBurnBuff);
            Debug.Log(
                $"ManaBurner: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, BurnPercent={burnPercent * 100}%"
            );
        }
    }

    private void RemoveManaBurnBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ManaBurnBuff)
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
