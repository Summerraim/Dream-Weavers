using UnityEngine;

/// <summary>
/// 射手羁绊：提升攻击力
/// (2) +15%攻击力
/// (4) +30%攻击力
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Archer")]
public class Archer : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoBonus = 0.15f; // 15%

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourBonus = 0.3f; // 30%

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        if (tier < 0)
        {
            RemoveArcherBuff(model);
            return;
        }

        float attackBonus = tier == 0 ? tierTwoBonus : tierFourBonus;

        // 移除旧的Buff
        RemoveArcherBuff(model);

        // 创建新的Buff
        var archerBuff = new ArcherBuff(model.Owner, attackBonus);

        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(archerBuff);
            Debug.Log(
                $"Archer: Applied to {model.Owner.DisplayName}, Tier={(tier + 1) * 2}, AttackBonus={attackBonus * 100}%"
            );
        }
    }

    private void RemoveArcherBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is ArcherBuff)
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
