using UnityEngine;

/// <summary>
/// 沉默Debuff - 增加技能法力消耗（注意：这是概念性实现，实际需要在技能使用时检查）
/// </summary>
public class SilenceDebuff : Buff
{
    public override string DisplayName => "沉默";
    public override string Description => "技能法力消耗增加";

    private float manaIncreaseMultiplier;

    public SilenceDebuff(IBattleUnit owner, int duration, float increase, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        manaIncreaseMultiplier = Mathf.Max(0f, increase);
    }

    public override void OnApplied()
    {
        Debug.Log($"{Owner?.DisplayName} 被沉默了！技能法力消耗增加 {manaIncreaseMultiplier * 100}%，持续 {RemainingTurns} 回合");
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 从沉默中恢复了");
    }

    public float GetManaIncreaseMultiplier()
    {
        return manaIncreaseMultiplier;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍被沉默，剩余 {RemainingTurns} 回合");
        }
    }
}
