using UnityEngine;

/// <summary>
/// 处决者羁绊：对低生命敌人造成额外伤害
/// (4) 敌方生命值低于10%时，造成一次登场宠物攻击力的额外攻击
/// (6) 触发阈值提升至20%
///
/// 重要：在Unity中创建此羁绊资源时，需要将Trigger Counts设置为[4, 6]
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Executioner")]
public class Executioner : Synergy
{
    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourThreshold = 0.1f; // 10%

    [Header("6层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierSixThreshold = 0.2f; // 20%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        // 处决者的档位以“队伍中拥有该羁绊的单位数量”为准：
        // - 4 个：10%
        // - 6 个：20%
        // 注意：这里不依赖 TriggerCounts 的具体配置，避免 [4,6] / [2,4,6] 等配置差异导致档位错位。
        int activeCount = model.ActiveCount;
        if (activeCount < 4)
        {
            RemoveExecutionerBuff(model);
            return;
        }

        float threshold = activeCount >= 6 ? tierSixThreshold : tierFourThreshold;

        // 移除旧的Buff
        RemoveExecutionerBuff(model);

        // 创建新的Buff
        var executionerBuff = new ExecutionerBuff(model.Owner, GetBattleModel(), threshold);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(executionerBuff);
            Debug.Log(
                $"Executioner: Applied to {model.Owner.DisplayName}, ActiveCount={activeCount}, Threshold={threshold * 100}%"
            );
        }
    }

    private void RemoveExecutionerBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ExecutionerBuff)
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
