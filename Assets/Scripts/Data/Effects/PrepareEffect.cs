using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Special/Prepare")]
// 准备效果：蓄力N回合后触发存储的所有效果
public class PrepareEffect : Effect
{
    [Header("准备设置")]
    [SerializeField, Min(1)]
    private int prepareTime = 3;

    [SerializeField]
    private bool applyToCaster = true;

    [Header("准备完成后触发的效果")]
    [SerializeField]
    private List<Effect> triggeredEffects = new List<Effect>();

    [SerializeField]
    private bool targetEnemy = true; // true=触发时目标是敌人，false=自己

    [Header("可选：准备期间的效果")]
    [SerializeField]
    private List<Effect> preparingEffects = new List<Effect>(); // 准备开始时立即触发的效果（如护盾）

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit owner = applyToCaster ? caster : target;
        IBattleUnit effectTarget = targetEnemy ? target : caster;

        if (owner == null || caster == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("PrepareEffect: No active battle model found");
            return;
        }

        // 先触发准备期间的效果（如给自己加护盾）
        if (preparingEffects != null && preparingEffects.Count > 0)
        {
            Debug.Log($"{owner.DisplayName} 准备技能时触发了 {preparingEffects.Count} 个辅助效果");
            foreach (var effect in preparingEffects)
            {
                if (effect != null)
                {
                    effect.Apply(caster, target);
                }
            }
        }

        // 创建准备中的Buff
        var preparingBuff = new PreparingBuff(
            owner,
            prepareTime,
            triggeredEffects,
            caster,
            effectTarget
        );

        CurrentBattle.AddBuff(preparingBuff);
    }
}
