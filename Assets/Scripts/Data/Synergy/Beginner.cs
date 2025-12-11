using UnityEngine;

/// <summary>
/// 初心者羁绊：首次技能释放不消耗MP且不进入冷却
/// (2) 两名初始心兽同时在场时，他们在每场战斗中的第一次技能释放不消耗MP，且该技能不进入冷却
///
/// 注意：此羁绊效果需要在BattleModel的技能释放逻辑中集成
/// 建议在技能释放前检查单位是否拥有BeginnerBuff且CanTriggerFreeSkill()为true
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Beginner")]
public class Beginner : Synergy
{
    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 初心者只有一个档位(2)
        if (tier < 0)
        {
            RemoveBeginnerBuff(model);
            return;
        }

        // 移除旧的Buff
        RemoveBeginnerBuff(model);

        // 创建新的Buff
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var beginnerBuff = new BeginnerBuff(model.Owner);
        battleModel.AddBuff(beginnerBuff);

        Debug.Log($"Beginner: Applied to {model.Owner.DisplayName}");
    }

    private void RemoveBeginnerBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is BeginnerBuff)
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
