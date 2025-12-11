using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祭品羁绊：阵亡后为队友提供随机增益
/// (3) 此羁绊宠物阵亡后，随机为其余非祭品队友施加以下一种效果：
///     A. 10%生命偷取
///     B. 20%最大生命值
///     C. 20%攻击力
///
/// 重要：在Unity中创建此羁绊资源时，需要将Trigger Counts设置为[3]
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Sacrifice")]
public class Sacrifice : Synergy
{
    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();

        // 祭品只有一个档位(3)，tier应该是0（表示第一个触发条件）
        // 如果tier < 0说明数量不足，没有达到任何档位
        if (tier < 0)
        {
            RemoveSacrificeBuff(model);
            return;
        }

        // 移除旧的Buff
        RemoveSacrificeBuff(model);

        // 获取所有上场的Spirit
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        // 需要获取所有Spirit数据，这里通过BattleController获取
        List<SpiritData> allSpirits = GetAllDeployedSpirits();
        if (allSpirits == null || allSpirits.Count == 0)
        {
            Debug.LogWarning("Sacrifice: Cannot get deployed spirits");
            return;
        }

        // 创建祭品Buff
        var sacrificeBuff = new SacrificeBuff(model.Owner, battleModel, allSpirits);
        battleModel.AddBuff(sacrificeBuff);

        Debug.Log($"Sacrifice: Applied to {model.Owner.DisplayName}");
    }

    private void RemoveSacrificeBuff(SynergyModel model)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return;

        var buffs = battleModel.GetBuffsForUnit(model.Owner);
        foreach (var buff in buffs)
        {
            if (buff is SacrificeBuff)
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

    private List<SpiritData> GetAllDeployedSpirits()
    {
        // 需要从BattleController获取所有Spirit
        // 这里假设有一个静态引用
        return SacrificeSynergyBridge.DeployedSpirits;
    }
}

/// <summary>
/// 用于桥接Sacrifice Synergy和BattleController的静态类
/// </summary>
public static class SacrificeSynergyBridge
{
    public static List<SpiritData> DeployedSpirits { get; set; }

    /// <summary>
    /// 委托：检查指定索引的Spirit是否存活
    /// </summary>
    public static System.Func<int, bool> IsSpiritAliveAtIndex { get; set; }

    /// <summary>
    /// 检查指定SpiritData是否存活
    /// </summary>
    public static bool IsSpiritAlive(SpiritData spiritData)
    {
        if (spiritData == null || DeployedSpirits == null || IsSpiritAliveAtIndex == null)
            return false;

        int index = DeployedSpirits.IndexOf(spiritData);
        if (index < 0)
            return false;

        return IsSpiritAliveAtIndex(index);
    }
}
