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

    // 桥接函数：用于访问所有Spirit的数据
    private readonly System.Func<List<SpiritData>> getDeployedSpirits;
    private readonly System.Func<int, SpiritRuntimeData> getSpiritRuntimeData;
    private readonly System.Action<int, int, int> saveSpiritHP;

    public HealAllBuff(IBattleUnit owner, int duration, BattleModel battleModel, float healPercent, SpiritData penalizedSpirit, float penalizePercent,
        System.Func<List<SpiritData>> getDeployedSpirits, System.Func<int, SpiritRuntimeData> getSpiritRuntimeData, System.Action<int, int, int> saveSpiritHP,
        Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        this.battleModel = battleModel;
        this.healPercent = Mathf.Clamp01(healPercent);
        this.penalizedSpirit = penalizedSpirit;
        this.penalizePercent = Mathf.Clamp01(penalizePercent);
        this.getDeployedSpirits = getDeployedSpirits;
        this.getSpiritRuntimeData = getSpiritRuntimeData;
        this.saveSpiritHP = saveSpiritHP;
        IsOneTime = true;
    }

    public override void OnApplied()
    {
        if (battleModel == null)
        {
            Debug.LogWarning("HealAllBuff: battleModel is null");
            return;
        }

        Debug.Log($"[HealAllBuff] OnApplied called, healPercent={healPercent}");

        // 方案1：治疗当前上场的Spirit（通过battleModel）
        if (battleModel.PlayerUnit != null)
        {
            var currentSpirit = battleModel.PlayerUnit;
            int amount = Mathf.CeilToInt(currentSpirit.MaxHP * healPercent);
            currentSpirit.ReceiveHeal(amount);
            Debug.Log($"[HealAllBuff] 当前上场Spirit {currentSpirit.DisplayName} 恢复 {amount} 生命值 (HP: {currentSpirit.HP}/{currentSpirit.MaxHP})");
        }

        // 方案2：治疗队列中所有其他Spirit（通过桥接函数）
        if (getDeployedSpirits != null && getSpiritRuntimeData != null && saveSpiritHP != null)
        {
            var deployedSpirits = getDeployedSpirits();
            if (deployedSpirits != null)
            {
                Debug.Log($"[HealAllBuff] 开始治疗队列中的所有Spirit，总数: {deployedSpirits.Count}");

                for (int i = 0; i < deployedSpirits.Count; i++)
                {
                    var spiritData = deployedSpirits[i];
                    var runtimeData = getSpiritRuntimeData(i);

                    if (runtimeData.CurrentHP <= 0)
                    {
                        Debug.Log($"[HealAllBuff] Spirit {i} ({spiritData.DisplayName}) 已死亡，跳过治疗");
                        continue;
                    }

                    // 计算治疗量
                    int healAmount = Mathf.CeilToInt(runtimeData.MaxHP * healPercent);
                    int newHP = Mathf.Min(runtimeData.CurrentHP + healAmount, runtimeData.MaxHP);

                    // 保存新的HP
                    saveSpiritHP(i, newHP, runtimeData.MaxHP);

                    Debug.Log($"[HealAllBuff] Spirit {i} ({spiritData.DisplayName}) 恢复 {healAmount} 生命值 (HP: {runtimeData.CurrentHP} -> {newHP}/{runtimeData.MaxHP})");
                }
            }
            else
            {
                Debug.LogWarning("[HealAllBuff] getDeployedSpirits returned null");
            }
        }
        else
        {
            Debug.LogWarning("[HealAllBuff] 桥接函数未设置，无法治疗队列中的Spirit");
        }

        // 查找并惩罚指定心兽（当前上场的）
        if (penalizedSpirit != null && battleModel.PlayerUnit is Spirit currentSpiritForPenalty)
        {
            if (currentSpiritForPenalty.Data == penalizedSpirit)
            {
                // 通过设置负向最大生命值加成来降低上限
                currentSpiritForPenalty.SetMaxHpBonusPercent(-Mathf.Abs(penalizePercent));
                Debug.Log($"[HealAllBuff] 特定心兽 {currentSpiritForPenalty.DisplayName} 最大生命值上限降低 {penalizePercent * 100}% ，当前上限 {currentSpiritForPenalty.MaxHP}");
            }
        }

        HasTriggered = true;
        Debug.Log($"[HealAllBuff] OnApplied finished");
    }
}
