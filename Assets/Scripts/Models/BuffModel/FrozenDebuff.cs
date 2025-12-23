using UnityEngine;

/// <summary>
/// 冰冻束缚 - 无法行动若干回合
/// </summary>
public class FrozenDebuff : Buff
{
    public override string DisplayName => "冰冻";
    public override string Description => "被冰冻，无法行动";
    public override bool IsStackable => true; // 允许叠加持续时间

    public FrozenDebuff(IBattleUnit owner, int duration, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
    }

    public override void OnApplied()
    {
        Debug.Log($"{Owner?.DisplayName} 被冰冻了！无法行动 {RemainingTurns} 回合");
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 从冰冻中解脱了");
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍被冰冻，剩余 {RemainingTurns} 回合");
        }
    }
}
