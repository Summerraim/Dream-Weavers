using UnityEngine;

/// <summary>
/// 角斗士羁绊：连续登场触发"角斗"效果
/// (3) 同一宠物连续登场6回合，触发"角斗"：
///     每次玩家回合开始时，随机扣除敌方最大生命值10*X（X∈1-6）%（无视无敌/免疫）
///
/// 重要：在Unity中创建此羁绊资源时，需要将Trigger Counts设置为[3]
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Gladiator")]
public class Gladiator : Synergy
{
    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 角斗士只有一个档位(3)，tier应该是0（表示第一个触发条件）
        // 如果tier < 0说明数量不足
        if (tier < 0)
        {
            RemoveGladiatorBuff(model);
            return;
        }

        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 检查是否已有角斗士Buff
        var existingBuff = FindGladiatorBuff(model.Owner);
        if (existingBuff != null)
        {
            // 已经有了，不需要重复添加
            return;
        }

        // 获取敌人
        IBattleUnit enemy = null;
        if (battleModel.EnemyUnits != null && battleModel.EnemyUnits.Count > 0)
        {
            enemy = battleModel.EnemyUnits[0];
        }

        if (enemy == null)
        {
            Debug.LogWarning("Gladiator: No enemy found");
            return;
        }

        // 创建角斗士Buff
        var gladiatorBuff = new GladiatorBuff(model.Owner, battleModel, enemy);
        battleModel.AddBuff(gladiatorBuff);

        Debug.Log($"Gladiator: Applied to {model.Owner.DisplayName}");
    }

    private GladiatorBuff FindGladiatorBuff(IBattleUnit owner)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return null;

        var buffs = battleModel.GetBuffsForUnit(owner);
        foreach (var buff in buffs)
        {
            if (buff is GladiatorBuff gladiatorBuff)
                return gladiatorBuff;
        }
        return null;
    }

    private void RemoveGladiatorBuff(SynergyModel model)
    {
        var gladiatorBuff = FindGladiatorBuff(model.Owner);
        if (gladiatorBuff != null)
        {
            var battleModel = GetBattleModel();
            battleModel?.RemoveBuff(gladiatorBuff);
        }
    }

    private BattleModel GetBattleModel()
    {
        return BerserkerSynergyBridge.CurrentBattle;
    }
}
