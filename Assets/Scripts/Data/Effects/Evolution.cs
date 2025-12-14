using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Evolution")]
/// <summary>
/// 精灵进化/替换 Effect：应用后使目标（通常为施法者/玩家单位）替换为指定 SpiritData。
/// </summary>
public class Evolution : Effect
{
    [Header("单一目标（可选，若使用映射则忽略）")]
    [SerializeField]
    private SpiritData targetSpirit;

    [SerializeField]
    private bool applyToCaster = true; // 为 true 时对施法者生效，否则对目标生效

    [SerializeField, Min(1)]
    private int dummyDuration = 1; // 兼容 Buff 构造，进化为一次性效果

    // 简化：不使用 DisplayName 映射与 PlayerData 检索，直接使用单一目标 targetSpirit

    [Header("羁绊→效果映射（一对一，简化版）")]
    [Tooltip("手动配置：当检测到某个羁绊已激活时，触发对应的 Effect（与下标一一对应）")]
    [SerializeField]
    private Synergy[] mappedSynergies;
    [SerializeField]
    private Effect[] mappedEffects;

    // 由外部在运行时注入当前已激活的羁绊集合（仅用于触发附加效果）
    private System.Collections.Generic.HashSet<Synergy> injectedActiveSynergies;

    // 与其它 Effect 保持一致：由外部在战斗开始或切换时注入
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (CurrentBattle == null)
        {
            Debug.LogWarning("Evolution: No active battle model found");
            return;
        }
        // 选择目标：直接使用单一目标 targetSpirit（不再依赖映射）
        SpiritData resolvedTarget = targetSpirit;
        if (resolvedTarget == null)
        {
            Debug.LogWarning("Evolution: targetSpirit not set");
            return;
        }

        // 接收者：优先施法者，否则目标；并在需要时回退到当前玩家单位，避免空引用
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
        {
            receiver = CurrentBattle.PlayerUnit;
            if (receiver == null)
            {
                Debug.LogWarning("Evolution: receiver is null and PlayerUnit is null");
                return;
            }
        }

        var buff = new EvolutionBuff(receiver, dummyDuration, CurrentBattle, resolvedTarget, this);
        CurrentBattle.AddBuff(buff);

        // 触发羁绊映射的附加效果（若符合条件）
        TryTriggerSynergyMappedEffects(receiver);
    }

    // 不再使用映射方法

    /// <summary>
    /// 外部注入：设置当前已激活的羁绊集合（通常来自你的羁绊系统判断结果）
    /// </summary>
    public void InjectActiveSynergies(System.Collections.Generic.IEnumerable<Synergy> synergies)
    {
        if (synergies == null)
        {
            injectedActiveSynergies = null;
            return;
        }
        injectedActiveSynergies = new System.Collections.Generic.HashSet<Synergy>(synergies);
    }

    private void TryTriggerSynergyMappedEffects(IBattleUnit receiver)
    {
        if (mappedSynergies == null || mappedEffects == null) return;
        int n = Mathf.Min(mappedSynergies.Length, mappedEffects.Length);
        if (n == 0) return;
        if (injectedActiveSynergies == null || injectedActiveSynergies.Count == 0) return;

        for (int i = 0; i < n; i++)
        {
            var s = mappedSynergies[i];
            var eff = mappedEffects[i];
            if (s == null || eff == null) continue;
            if (!injectedActiveSynergies.Contains(s)) continue;
            try
            {
                eff.Apply(receiver, receiver);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Evolution: Trigger effect failed for synergy '{s?.name}': {ex.Message}");
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 简单校验：两个数组长度需一致
        if (mappedSynergies != null && mappedEffects != null && mappedSynergies.Length != mappedEffects.Length)
        {
            Debug.LogWarning($"[Evolution] 映射长度不一致：mappedSynergies={mappedSynergies.Length}, mappedEffects={mappedEffects.Length}。索引需一一对应。");
        }
    }
#endif
}

// 已删除 EvolutionMapping：由你直接配置 targetSpirit 即可

// 不再使用单独的映射类；改为两个并行数组在 Evolution 中配置

// 合并完成：不再存在第二个类声明
