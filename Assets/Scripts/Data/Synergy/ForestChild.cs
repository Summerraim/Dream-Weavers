using UnityEngine;

/// <summary>
/// 森林之子羁绊：增加最大生命值和法力值，6档额外获得生命偷取
/// (2) +10%最大生命值，+10%最大法力值
/// (4) +20%最大生命值，+20%最大法力值
/// (6) +30%最大生命值，+30%最大法力值，+10%生命偷取
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/ForestChild")]
public class ForestChild : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoHpBonus = 0.1f; // 10%

    [SerializeField, Range(0f, 1f)]
    private float tierTwoManaBonus = 0.1f; // 10%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourHpBonus = 0.2f; // 20%

    [SerializeField, Range(0f, 1f)]
    private float tierFourManaBonus = 0.2f; // 20%

    [Header("6层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierSixHpBonus = 0.3f; // 30%

    [SerializeField, Range(0f, 1f)]
    private float tierSixManaBonus = 0.3f; // 30%

    [SerializeField, Range(0f, 1f)]
    private float tierSixLifeSteal = 0.1f; // 10%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveForestChildBuff(model);
            return;
        }

        float hpBonus = 0f;
        float manaBonus = 0f;
        float lifeSteal = 0f;

        // 根据档位设置加成
        switch (tier)
        {
            case 0: // 2人
                hpBonus = tierTwoHpBonus;
                manaBonus = tierTwoManaBonus;
                break;
            case 1: // 4人
                hpBonus = tierFourHpBonus;
                manaBonus = tierFourManaBonus;
                break;
            case 2: // 6人
                hpBonus = tierSixHpBonus;
                manaBonus = tierSixManaBonus;
                lifeSteal = tierSixLifeSteal;
                break;
        }

        // 移除旧的Buff
        RemoveForestChildBuff(model);

        // 创建新的Buff
        var forestChildBuff = new ForestChildBuff(model.Owner, hpBonus, manaBonus, lifeSteal);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(forestChildBuff);
            Debug.Log(
                $"ForestChild: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, HP+{hpBonus * 100}%, Mana+{manaBonus * 100}%, LifeSteal+{lifeSteal * 100}%"
            );
        }
    }

    private void RemoveForestChildBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ForestChildBuff)
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
