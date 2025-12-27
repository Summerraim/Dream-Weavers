using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Heal")]
//治疗:总治疗 = flatHealing + (接受者最大HP × percentMaxHP)
//如果 applyToCaster 为 true，治疗施法者；否则治疗目标
//如果 healAllDeployedSpirits 为 true，治疗所有已部署的精灵
public class Heal : Effect
{
    [SerializeField, Min(0)]
    private int initHealing = 40;

    [SerializeField, Range(0f, 1f)]
    private float percentMaxHP = 0.1f;

    [SerializeField]
    private bool applyToCaster = false;

    [SerializeField, Tooltip("是否治疗所有已部署的精灵（优先级高于applyToCaster）")]
    private bool healAllDeployedSpirits = false;

    // 桥接属性：访问战斗系统
    public static BattleModel CurrentBattle { get; set; }
    public static System.Func<List<SpiritData>> GetDeployedSpirits { get; set; }
    public static System.Func<int, SpiritRuntimeData> GetSpiritRuntimeData { get; set; }
    public static System.Action<int, int, int> SaveSpiritHP { get; set; } // index, currentHP, maxHP

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        // 如果选择治疗所有已部署的精灵
        if (healAllDeployedSpirits)
        {
            HealAllDeployedSpirits(caster);
            return;
        }

        // 原有逻辑：治疗单个目标
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        int totalHealing = CalculateHealing(receiver.MaxHP);
        if (totalHealing <= 0)
            return;

        receiver.ReceiveHeal(totalHealing);
    }

    /// <summary>
    /// 治疗所有已部署的精灵
    /// </summary>
    private void HealAllDeployedSpirits(IBattleUnit caster)
    {
        if (GetDeployedSpirits == null || GetSpiritRuntimeData == null || SaveSpiritHP == null)
        {
            Debug.LogWarning("Heal: 桥接方法未设置，无法治疗所有已部署的精灵");
            return;
        }

        var deployedSpirits = GetDeployedSpirits();
        if (deployedSpirits == null || deployedSpirits.Count == 0)
        {
            Debug.LogWarning("Heal: 没有已部署的精灵");
            return;
        }

        // 找到当前战斗中的精灵索引
        int currentSpiritIndex = -1;
        if (caster is Spirit currentSpirit && currentSpirit.Data != null)
        {
            for (int i = 0; i < deployedSpirits.Count; i++)
            {
                if (deployedSpirits[i] == currentSpirit.Data)
                {
                    currentSpiritIndex = i;
                    break;
                }
            }
        }

        // 遍历所有已部署的精灵并治疗
        for (int i = 0; i < deployedSpirits.Count; i++)
        {
            var spiritData = deployedSpirits[i];
            if (spiritData == null)
                continue;

            // 如果是当前战斗中的精灵，直接治疗
            if (i == currentSpiritIndex && caster != null)
            {
                int totalHealing = CalculateHealing(caster.MaxHP);
                if (totalHealing > 0)
                {
                    caster.ReceiveHeal(totalHealing);
                    Debug.Log($"Heal: 治疗当前精灵 {spiritData.DisplayName}: +{totalHealing} HP");
                }
            }
            else
            {
                // 治疗其他已部署但不在战斗中的精灵
                var runtimeData = GetSpiritRuntimeData(i);
                int totalHealing = CalculateHealing(runtimeData.MaxHP);
                if (totalHealing > 0)
                {
                    int newHP = Mathf.Min(runtimeData.CurrentHP + totalHealing, runtimeData.MaxHP);
                    SaveSpiritHP(i, newHP, runtimeData.MaxHP);
                    Debug.Log($"Heal: 治疗精灵 {spiritData.DisplayName}: +{totalHealing} HP ({runtimeData.CurrentHP} -> {newHP})");
                }
            }
        }
    }

    /// <summary>
    /// 计算治疗量
    /// </summary>
    private int CalculateHealing(int receiverMaxHP)
    {
        int totalHealing = Mathf.Max(0, initHealing);
        if (percentMaxHP > 0f && receiverMaxHP > 0)
        {
            totalHealing += Mathf.CeilToInt(receiverMaxHP * Mathf.Clamp01(percentMaxHP));
        }
        return totalHealing;
    }
}
