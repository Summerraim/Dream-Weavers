using UnityEngine;

/// <summary>
/// 虚空遗民羁绊：攻击和技能无视敌人部分防御力
/// (2) 无视敌人15%的防御力
/// (4) 无视敌人25%的防御力
/// 注意：由于战斗中同时只有一个Spirit在场，所以4档只是提升无视防御的百分比
///
/// 此羁绊效果需要在BattleModel的伤害计算中检查VoidExileBuff
/// 建议在计算伤害时，检查攻击者是否拥有VoidExileBuff，并相应减少防御力计算
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/VoidExile")]
public class VoidExile : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoIgnoreDefense = 0.15f; // 15%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourIgnoreDefense = 0.25f; // 25%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveVoidExileBuff(model);
            return;
        }

        float ignoreDefense = tier == 0 ? tierTwoIgnoreDefense : tierFourIgnoreDefense;

        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 移除旧的Buff
        RemoveVoidExileBuff(model);

        // 添加新的Buff
        var voidBuff = new VoidExileBuff(model.Owner, ignoreDefense);
        battleModel.AddBuff(voidBuff);

        Debug.Log(
            $"VoidExile: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, IgnoreDefense={ignoreDefense * 100}%"
        );
    }

    private void RemoveVoidExileBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is VoidExileBuff)
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
