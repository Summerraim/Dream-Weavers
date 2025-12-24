using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 祭品Buff：阵亡后为其余非祭品队友施加随机效果
/// </summary>
public class SacrificeBuff : Buff
{
    public override string DisplayName => "祭品";
    public override string Description => "阵亡后为队友提供随机增益：生命偷取/最大生命值/攻击力";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private BattleModel battleModel;
    private List<SpiritData> allSpirits; // 所有上场的Spirit

    public SacrificeBuff(IBattleUnit owner, BattleModel battleModel, List<SpiritData> allSpirits)
        : base(owner, -1) // 永久Buff
    {
        this.battleModel = battleModel;
        this.allSpirits = allSpirits;
    }

    public override bool OnDeath()
    {
        Debug.Log(
            $"SacrificeBuff: {Owner?.DisplayName} is sacrificed, granting buffs to teammates"
        );

        if (battleModel == null || allSpirits == null)
            return false;

        // 找到所有非祭品的队友
        List<IBattleUnit> validTeammates = new List<IBattleUnit>();

        foreach (var spiritData in allSpirits)
        {
            // 检查是否有祭品羁绊
            bool hasSacrifice = spiritData.Synergies.Any(s => s.SynergyId == "Sacrifice");

            // 跳过自己和其他祭品
            if (spiritData.DisplayName == Owner.DisplayName || hasSacrifice)
                continue;

            // 需要找到对应的IBattleUnit实例
            // 这里假设PlayerUnit就是Spirit类型
            if (
                battleModel.PlayerUnit != null
                && battleModel.PlayerUnit.DisplayName == spiritData.DisplayName
            )
            {
                validTeammates.Add(battleModel.PlayerUnit);
            }
        }

        // 为每个有效队友随机施加一个效果
        foreach (var teammate in validTeammates)
        {
            int randomChoice = Random.Range(0, 3);
            Buff buffToApply = null;

            switch (randomChoice)
            {
                case 0: // 10%生命偷取
                    buffToApply = new LifeStealBuff(teammate, 0.1f);
                    Debug.Log($"SacrificeBuff: Granting Life Steal to {teammate.DisplayName}");
                    break;
                case 1: // 20%最大生命值
                    buffToApply = new MaxHealthBuff(teammate, 0.2f);
                    Debug.Log($"SacrificeBuff: Granting Max Health to {teammate.DisplayName}");
                    break;
                case 2: // 20%攻击力
                    buffToApply = new AttackBuff(teammate, 0.2f);
                    Debug.Log($"SacrificeBuff: Granting Attack Power to {teammate.DisplayName}");
                    break;
            }

            if (buffToApply != null)
            {
                battleModel.AddBuff(buffToApply);
            }
        }

        // 不阻止死亡
        return false;
    }

    public override void OnApplied()
    {
        Debug.Log($"SacrificeBuff: Applied to {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
