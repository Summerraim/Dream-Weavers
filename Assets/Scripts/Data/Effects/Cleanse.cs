using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 净化效果：随机移除目标身上的一个Debuff
/// </summary>
[CreateAssetMenu(menuName = "Data/Effects/Base/Cleanse")]
public class Cleanse : Effect
{
    [SerializeField]
    private bool applyToCaster = false;

    [SerializeField, Min(1)]
    private int maxDebuffsToRemove = 1;

    [SerializeField]
    private bool removeAllDebuffs = false;

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("Cleanse: No active battle model found");
            return;
        }

        // 获取目标单位的所有Buff
        var allBuffs = CurrentBattle.GetBuffsForUnit(receiver);
        if (allBuffs == null || allBuffs.Count == 0)
        {
            Debug.Log($"Cleanse: {receiver.DisplayName} has no buffs to cleanse");
            return;
        }

        // 筛选出Debuff（通过检查类型名是否包含"Debuff"）
        List<Buff> debuffs = new List<Buff>();
        foreach (var buff in allBuffs)
        {
            if (IsDebuff(buff))
            {
                debuffs.Add(buff);
            }
        }

        if (debuffs.Count == 0)
        {
            Debug.Log($"Cleanse: {receiver.DisplayName} has no debuffs to cleanse");
            return;
        }

        // 决定移除多少个Debuff
        int debuffsToRemove = removeAllDebuffs
            ? debuffs.Count
            : Mathf.Min(maxDebuffsToRemove, debuffs.Count);

        // 随机移除Debuff
        for (int i = 0; i < debuffsToRemove; i++)
        {
            if (debuffs.Count == 0)
                break;

            int randomIndex = Random.Range(0, debuffs.Count);
            Buff debuffToRemove = debuffs[randomIndex];

            CurrentBattle.RemoveBuff(debuffToRemove);
            Debug.Log($"Cleanse: Removed {debuffToRemove.DisplayName} from {receiver.DisplayName}");

            debuffs.RemoveAt(randomIndex);
        }
    }

    /// <summary>
    /// 判断一个Buff是否为Debuff
    /// 通过检查类名是否包含"Debuff"来判断
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
