using UnityEngine;

/// <summary>
/// 全能化身羁绊：死亡时回复生命值和法力值
/// (1) 每次死亡时回复10%最大生命值和法力值，最多可触发三次
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Omnipotent")]
public class Omnipotent : Synergy
{
    [Header("配置")]
    [SerializeField, Range(0f, 1f)]
    private float revivePercent = 0.1f; // 10%

    [SerializeField, Range(1, 10)]
    private int maxRevives = 3; // 最多3次

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 全能化身只有一个档位(1)
        if (tier < 0)
        {
            RemoveOmnipotentBuff(model);
            return;
        }

        // 移除旧的Buff
        RemoveOmnipotentBuff(model);

        // 创建新的Buff
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var omnipotentBuff = new OmnipotentBuff(model.Owner, revivePercent, maxRevives);
        battleModel.AddBuff(omnipotentBuff);

        Debug.Log(
            $"Omnipotent: Applied to {model.Owner.DisplayName}, {revivePercent * 100}% revive, {maxRevives} times"
        );
    }

    private void RemoveOmnipotentBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is OmnipotentBuff)
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
