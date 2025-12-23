using UnityEngine;

/// <summary>
/// 混乱Debuff - 有几率攻击自己（注意：这是概念性实现，实际需要在战斗逻辑中检查）
/// </summary>
public class ConfusionDebuff : Buff
{
    public override string DisplayName => "混乱";
    public override string Description => "陷入混乱，可能攻击自己";
    public override bool IsStackable => true; // 允许叠加持续时间

    private float confusionChance;

    public ConfusionDebuff(IBattleUnit owner, int duration, float chance)
        : base(owner, duration)
    {
        confusionChance = Mathf.Clamp01(chance);
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"{Owner?.DisplayName} 陷入了混乱！有 {confusionChance * 100}% 几率攻击自己，持续 {RemainingTurns} 回合"
        );
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 从混乱中清醒了");
    }

    /// <summary>
    /// 检查是否触发混乱（在使用技能时调用）
    /// </summary>
    /// <returns>true表示混乱触发，应该攻击自己</returns>
    public bool CheckConfusion()
    {
        float roll = Random.Range(0f, 1f);
        bool confused = roll < confusionChance;

        if (confused)
        {
            Debug.Log($"{Owner?.DisplayName} 混乱发作，攻击了自己！");
        }

        return confused;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍处于混乱，剩余 {RemainingTurns} 回合");
        }
    }
}
