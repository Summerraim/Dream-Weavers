using UnityEngine;

/// <summary>
/// 派对狂欢羁绊：释放技能时有几率不进入冷却
/// (3) 每次派对狂欢单位释放技能时，都有20%概率没有冷却回合
///
/// 注意：此羁绊效果需要在BattleModel的技能释放逻辑中集成
/// 建议在技能释放后检查单位是否拥有PartyTimeBuff，并调用TryTriggerNoCooldown()
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/PartyTime")]
public class PartyTime : Synergy
{
    [Header("配置")]
    [SerializeField, Range(0f, 1f)]
    private float noCooldownChance = 0.2f; // 20%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 派对狂欢只有一个档位(3)
        if (tier < 0)
        {
            RemovePartyTimeBuff(model);
            return;
        }

        // 移除旧的Buff
        RemovePartyTimeBuff(model);

        // 创建新的Buff
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var partyTimeBuff = new PartyTimeBuff(model.Owner, noCooldownChance);
        battleModel.AddBuff(partyTimeBuff);

        Debug.Log(
            $"PartyTime: Applied to {model.Owner.DisplayName}, NoCooldownChance={noCooldownChance * 100}%"
        );
    }

    private void RemovePartyTimeBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is PartyTimeBuff)
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
