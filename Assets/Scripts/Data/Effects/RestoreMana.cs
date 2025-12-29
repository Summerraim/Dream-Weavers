using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/RestoreMana")]
//法力恢复:总恢复 = flatManaRestore + (接受者最大MP × percentMaxMP)
//如果 applyToCaster 为 true，恢复施法者；否则恢复目标
//如果 restoreAllDeployedSpirits 为 true，恢复所有已部署的精灵
public class RestoreMana : Effect
{
    [SerializeField, Min(0)]
    private int flatManaRestore = 30;

    [SerializeField, Range(0f, 1f)]
    private float percentMaxMP = 0.1f;

    [SerializeField]
    private bool applyToCaster = false;

    [SerializeField, Tooltip("是否恢复所有已部署精灵的法力值（优先级高于applyToCaster）")]
    private bool restoreAllDeployedSpirits = false;

    // 桥接属性：访问战斗系统
    public static BattleModel CurrentBattle { get; set; }
    public static System.Func<List<SpiritData>> GetDeployedSpirits { get; set; }
    public static System.Func<int, SpiritRuntimeData> GetSpiritRuntimeData { get; set; }
    public static System.Action<int, int, int> SaveSpiritMP { get; set; } // index, currentMP, maxMP

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        // 如果选择恢复所有已部署的精灵
        if (restoreAllDeployedSpirits)
        {
            RestoreManaAllDeployedSpirits(caster);
            return;
        }

        // 原有逻辑：恢复单个目标
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        int totalRestore = CalculateManaRestore(receiver.MaxMana);
        if (totalRestore <= 0)
            return;

        receiver.RestoreMana(totalRestore);
    }

    /// <summary>
    /// 恢复所有已部署精灵的法力值
    /// </summary>
    private void RestoreManaAllDeployedSpirits(IBattleUnit caster)
    {
        if (GetDeployedSpirits == null || GetSpiritRuntimeData == null || SaveSpiritMP == null)
        {
            Debug.LogWarning("RestoreMana: 桥接方法未设置，无法恢复所有已部署精灵的法力值");
            return;
        }

        var deployedSpirits = GetDeployedSpirits();
        if (deployedSpirits == null || deployedSpirits.Count == 0)
        {
            Debug.LogWarning("RestoreMana: 没有已部署的精灵");
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

        // 遍历所有已部署的精灵并恢复法力值
        for (int i = 0; i < deployedSpirits.Count; i++)
        {
            var spiritData = deployedSpirits[i];
            if (spiritData == null)
                continue;

            // 如果是当前战斗中的精灵，直接恢复
            if (i == currentSpiritIndex && caster != null)
            {
                int totalRestore = CalculateManaRestore(caster.MaxMana);
                if (totalRestore > 0)
                {
                    caster.RestoreMana(totalRestore);
                    Debug.Log($"RestoreMana: 恢复当前精灵 {spiritData.DisplayName} 法力值: +{totalRestore} MP");
                }
            }
            else
            {
                // 恢复其他已部署但不在战斗中的精灵
                var runtimeData = GetSpiritRuntimeData(i);
                int totalRestore = CalculateManaRestore(runtimeData.MaxMP);
                if (totalRestore > 0)
                {
                    int newMP = Mathf.Min(runtimeData.CurrentMP + totalRestore, runtimeData.MaxMP);
                    SaveSpiritMP(i, newMP, runtimeData.MaxMP);
                    Debug.Log($"RestoreMana: 恢复精灵 {spiritData.DisplayName} 法力值: +{totalRestore} MP ({runtimeData.CurrentMP} -> {newMP})");
                }
            }
        }
    }

    /// <summary>
    /// 计算法力恢复量
    /// </summary>
    private int CalculateManaRestore(int receiverMaxMP)
    {
        int totalRestore = Mathf.Max(0, flatManaRestore);
        if (percentMaxMP > 0f && receiverMaxMP > 0)
        {
            totalRestore += Mathf.CeilToInt(receiverMaxMP * Mathf.Clamp01(percentMaxMP));
        }
        return totalRestore;
    }
}
