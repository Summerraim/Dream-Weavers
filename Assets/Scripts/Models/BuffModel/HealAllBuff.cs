using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一次性 Buff：回复所有场上精灵一定百分比生命值；
/// 若队伍中存在指定的心兽（SpiritData），则该心兽扣除一定百分比生命值。
/// </summary>
public class HealAllBuff : Buff
{
    public override string DisplayName => "群体治疗";
    public override string Description => "回复所有精灵生命值并惩罚特定心兽";

    private readonly BattleModel battleModel;
    private readonly float healPercent;   // 0..1
    private readonly SpiritData penalizedSpirit; // 特定心兽
    private readonly float penalizePercent; // 0..1

    public HealAllBuff(IBattleUnit owner, int duration, BattleModel battleModel, float healPercent, SpiritData penalizedSpirit, float penalizePercent, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        this.battleModel = battleModel;
        this.healPercent = Mathf.Clamp01(healPercent);
        this.penalizedSpirit = penalizedSpirit;
        this.penalizePercent = Mathf.Clamp01(penalizePercent);
        IsOneTime = true;
    }

    public override void OnApplied()
    {
        if (battleModel == null)
        {
            Debug.LogWarning("HealAllBuff: battleModel is null");
            return;
        }

        // 收集场上所有单位：玩家 + 敌人
        var units = new List<IBattleUnit>();
        if (battleModel.PlayerUnit != null) units.Add(battleModel.PlayerUnit);
        if (battleModel.EnemyUnits != null)
        {
            for (int i = 0; i < battleModel.EnemyUnits.Count; i++)
            {
                var enemy = battleModel.EnemyUnits[i];
                if (enemy != null) units.Add(enemy);
            }
        }

        // 先执行群体治疗
        foreach (var unit in units)
        {
            int amount = 0;
            // 依据 MaxHP 百分比治疗
            if (unit is Spirit spiritUnit)
            {
                amount = Mathf.CeilToInt(spiritUnit.MaxHP * healPercent);
            }
            else
            {
                amount = Mathf.CeilToInt(unit.MaxHP * healPercent);
            }
            unit.ReceiveHeal(amount);
            Debug.Log($"HealAllBuff: {unit.DisplayName} 恢复 {amount} 生命值");
        }

        // 查找并惩罚指定心兽：仅在队伍中存在时生效（扣除最大生命值上限30%）
        if (penalizedSpirit != null)
        {
            foreach (var unit in units)
            {
                // 只针对 Spirit 类型进行匹配
                if (unit is Spirit s && s.Data == penalizedSpirit)
                {
                    // 通过设置负向最大生命值加成来降低上限
                    s.SetMaxHpBonusPercent(-Mathf.Abs(penalizePercent));
                    Debug.Log($"HealAllBuff: 特定心兽 {s.DisplayName} 最大生命值上限降低 {penalizePercent * 100}% ，当前上限 {s.MaxHP}");
                }
            }
        }

        HasTriggered = true;
    }
}
