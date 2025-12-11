using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 疗愈者Buff：每次释放技能时随机治疗一个队友
/// </summary>
public class HealerBuff : Buff
{
    public override string DisplayName => "疗愈者";
    public override string Description => $"释放技能时随机治疗队友{healPercent * 100}%最大生命值";

    private float healPercent; // 治疗百分比
    private List<SpiritData> allSpirits; // 所有上场的Spirit

    public HealerBuff(IBattleUnit owner, float healPercent, List<SpiritData> allSpirits)
        : base(owner, -1) // 永久Buff
    {
        this.healPercent = healPercent;
        this.allSpirits = allSpirits;
    }

    /// <summary>
    /// 触发治疗效果（由BattleController在使用技能后调用）
    /// </summary>
    /// <param name="battleModel">战斗模型，用于获取队友信息</param>
    public void TriggerHeal(BattleModel battleModel)
    {
        if (battleModel == null || allSpirits == null || allSpirits.Count == 0)
            return;

        // 获取所有可能的治疗目标（包括当前Spirit和其他Spirit）
        List<IBattleUnit> healTargets = new List<IBattleUnit>();

        // 添加当前登场的Spirit
        if (battleModel.PlayerUnit != null)
        {
            healTargets.Add(battleModel.PlayerUnit);
        }

        // TODO: 如果有其他队友在场，也添加进来
        // 目前只治疗当前Spirit，因为其他Spirit可能在后备队列中

        if (healTargets.Count == 0)
        {
            Debug.LogWarning("HealerBuff: No valid heal targets");
            return;
        }

        // 随机选择一个目标
        int randomIndex = Random.Range(0, healTargets.Count);
        IBattleUnit target = healTargets[randomIndex];

        // 计算治疗量
        int healAmount = Mathf.CeilToInt(target.MaxHP * healPercent);

        // 执行治疗
        target.ReceiveHeal(healAmount);

        Debug.Log(
            $"HealerBuff: {Owner?.DisplayName} heals {target.DisplayName} for {healAmount} HP ({healPercent * 100}% of max HP)"
        );
    }

    public override void OnApplied()
    {
        Debug.Log($"HealerBuff: Applied to {Owner?.DisplayName}, heal percent: {healPercent * 100}%");
    }

    public override void OnRemoved()
    {
        Debug.Log($"HealerBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
