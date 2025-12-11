using UnityEngine;

/// <summary>
/// 学者羁绊：提升最大法力值
/// (2) +20%最大法力值
/// (4) +40%最大法力值
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Scholar")]
public class Scholar : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoBonus = 0.2f; // 20%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourBonus = 0.4f; // 40%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveScholarBuff(model);
            return;
        }

        float manaBonus = tier == 0 ? tierTwoBonus : tierFourBonus;

        // 移除旧的Buff
        RemoveScholarBuff(model);

        // 创建新的Buff
        var scholarBuff = new ScholarBuff(model.Owner, manaBonus);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(scholarBuff);
            Debug.Log(
                $"Scholar: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, ManaBonus={manaBonus * 100}%"
            );
        }
    }

    private void RemoveScholarBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ScholarBuff)
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
