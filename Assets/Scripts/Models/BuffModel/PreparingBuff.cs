using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 准备中Buff - 倒计时结束后触发存储的效果
/// </summary>
public class PreparingBuff : Buff
{
    public override string DisplayName => "准备中";
    public override string Description => "正在准备强力技能";

    private List<Effect> effectsToTrigger;
    private IBattleUnit caster;
    private IBattleUnit target;
    private bool hasTriggered = false;

    public PreparingBuff(
        IBattleUnit owner,
        int duration,
        List<Effect> effects,
        IBattleUnit originalCaster,
        IBattleUnit originalTarget
    ) : base(owner, duration)
    {
        // 深拷贝Effect列表
        effectsToTrigger = effects != null ? new List<Effect>(effects) : new List<Effect>();
        caster = originalCaster;
        target = originalTarget;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"<color=cyan>{Owner?.DisplayName} 开始准备技能！需要 {RemainingTurns} 回合，将触发 {effectsToTrigger.Count} 个效果</color>"
        );
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd(); // 减少剩余回合数

        if (RemainingTurns > 0)
        {
            Debug.Log(
                $"<color=yellow>{Owner?.DisplayName} 准备中... 剩余 {RemainingTurns} 回合</color>"
            );
        }
        else if (!hasTriggered)
        {
            // 准备完成！触发所有Effect
            TriggerEffects();
            hasTriggered = true;
        }
    }

    private void TriggerEffects()
    {
        Debug.Log(
            $"<color=green>★ {Owner?.DisplayName} 准备完成！释放蓄力技能！ ★</color>"
        );

        if (effectsToTrigger == null || effectsToTrigger.Count == 0)
        {
            Debug.LogWarning("PreparingBuff: 没有要触发的效果！");
            return;
        }

        // 检查施法者和目标是否仍然有效
        if (caster == null || caster.IsDead)
        {
            Debug.Log($"{Owner?.DisplayName} 的蓄力技能因施法者失效而取消");
            return;
        }

        if (target == null || target.IsDead)
        {
            Debug.Log($"{Owner?.DisplayName} 的蓄力技能因目标失效而取消");
            return;
        }

        // 遍历并触发所有存储的Effect
        int successCount = 0;
        foreach (var effect in effectsToTrigger)
        {
            if (effect != null)
            {
                Debug.Log(
                    $"  → 触发效果: {effect.DisplayName}"
                );
                effect.Apply(caster, target);
                successCount++;
            }
        }

        Debug.Log(
            $"<color=green>{Owner?.DisplayName} 成功触发了 {successCount}/{effectsToTrigger.Count} 个效果！</color>"
        );
    }

    public override void OnRemoved()
    {
        if (!hasTriggered && RemainingTurns > 0)
        {
            Debug.Log(
                $"<color=red>{Owner?.DisplayName} 的准备被提前中断了！</color>"
            );
        }
        else
        {
            Debug.Log($"{Owner?.DisplayName} 的准备状态结束");
        }
    }

    /// <summary>
    /// 被攻击时可以选择打断准备（可选功能）
    /// </summary>
    public override void OnDamageReceived(int actualDamage, IBattleUnit attacker)
    {
        // 可选：受到大量伤害时打断准备
        // if (actualDamage > Owner.MaxHP * 0.2f)
        // {
        //     Debug.Log($"{Owner?.DisplayName} 受到重击，准备被打断！");
        //     // 通过BattleModel移除自己
        // }
    }

    /// <summary>
    /// 获取准备进度百分比（用于UI显示）
    /// </summary>
    public float GetProgress(int totalDuration)
    {
        if (totalDuration <= 0)
            return 1f;
        return 1f - ((float)RemainingTurns / totalDuration);
    }
}
