using UnityEngine;

/// <summary>
/// 不朽造物羁绊：免疫即死效果，首次濒死时获得无敌
/// (1) 免疫即死效果，并且在首次生命值降至1点时，会获得一个持续2回合的无敌效果（每场战斗一次）
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Immortal")]
public class Immortal : Synergy
{
    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 不朽造物只有一个档位(1)
        if (tier < 0)
        {
            RemoveImmortalBuff(model);
            return;
        }

        // 移除旧的Buff
        RemoveImmortalBuff(model);

        // 创建新的Buff
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var immortalBuff = new ImmortalBuff(model.Owner, battleModel);
        battleModel.AddBuff(immortalBuff);

        Debug.Log($"Immortal: Applied to {model.Owner.DisplayName}");
    }

    private void RemoveImmortalBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ImmortalBuff)
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
