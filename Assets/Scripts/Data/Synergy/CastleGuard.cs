using UnityEngine;

/// <summary>
/// 城堡卫队羁绊：战斗开始时提供护盾
/// (2) 战斗开始时，为自身提供15%攻击力的护盾
/// (4) 护盾值提升至25%攻击力
/// 注意：由于战斗中同时只有一个Spirit在场，所以4档只是提升护盾值，而不是为"所有友军"提供
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/CastleGuard")]
public class CastleGuard : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoShieldPercent = 0.15f; // 15%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourShieldPercent = 0.25f; // 25%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveCastleGuardBuff(model);
            return;
        }

        float shieldPercent = tier == 0 ? tierTwoShieldPercent : tierFourShieldPercent;

        // 移除旧的Buff
        RemoveCastleGuardBuff(model);

        // 获取BattleModel
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 创建新的Buff
        var castleGuardBuff = new CastleGuardBuff(model.Owner, shieldPercent, battleModel);
        battleModel.AddBuff(castleGuardBuff);

        Debug.Log(
            $"CastleGuard: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, Shield={shieldPercent * 100}%"
        );
    }

    private void RemoveCastleGuardBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is CastleGuardBuff)
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
