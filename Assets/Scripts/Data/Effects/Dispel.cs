using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 驱散效果：随机移除目标身上的一个Buff（增益效果）
/// 与Cleanse相反，用于移除敌人的增益效果
/// </summary>
[CreateAssetMenu(menuName = "Data/Effects/Base/Dispel")]
public class Dispel : Effect
{
    [SerializeField]
    private bool applyToTarget = true;

    [SerializeField, Min(1)]
    private int maxBuffsToRemove = 1;

    [SerializeField]
    private bool removeAllBuffs = false;

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToTarget ? target : caster;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("Dispel: No active battle model found");
            return;
        }

        // 获取目标单位的所有Buff
        var allBuffs = CurrentBattle.GetBuffsForUnit(receiver);
        if (allBuffs == null || allBuffs.Count == 0)
        {
            Debug.Log($"Dispel: {receiver.DisplayName} has no buffs to dispel");
            return;
        }

        // 筛选出增益Buff（不包含Debuff）
        List<Buff> positiveBuffs = new List<Buff>();
        foreach (var buff in allBuffs)
        {
            if (!IsDebuff(buff))
            {
                positiveBuffs.Add(buff);
            }
        }

        if (positiveBuffs.Count == 0)
        {
            Debug.Log($"Dispel: {receiver.DisplayName} has no positive buffs to dispel");
            return;
        }

        // 决定移除多少个Buff
        int buffsToRemove = removeAllBuffs ? positiveBuffs.Count : Mathf.Min(maxBuffsToRemove, positiveBuffs.Count);

        // 随机移除Buff
        for (int i = 0; i < buffsToRemove; i++)
        {
            if (positiveBuffs.Count == 0)
                break;

            int randomIndex = Random.Range(0, positiveBuffs.Count);
            Buff buffToRemove = positiveBuffs[randomIndex];

            CurrentBattle.RemoveBuff(buffToRemove);
            Debug.Log($"Dispel: Removed {buffToRemove.DisplayName} from {receiver.DisplayName}");

            positiveBuffs.RemoveAt(randomIndex);
        }
    }

    /// <summary>
    /// 判断一个Buff是否为Debuff
    /// </summary>
    private bool IsDebuff(Buff buff)
    {
        if (buff == null)
            return false;

        // 方法1: 检查类名
        string typeName = buff.GetType().Name;
        if (typeName.Contains("Debuff"))
            return true;

        // 方法2: 检查已知的Debuff类型
        return buff is PoisonDebuff
            || buff is BurnDebuff
            || buff is BlindDebuff
            || buff is SilenceDebuff
            || buff is SleepDebuff
            || buff is FrozenDebuff
            || buff is ConfusionDebuff
            || buff is WeakenAttackDebuff
            || buff is WeakenDefenseDebuff
            || buff is WeakenBuff
            || buff is VulnerabilityDebuff
            || buff is HealingReductionDebuff
            || buff is ManaLeechDebuff
            || buff is CurseDebuff;
    }
}
