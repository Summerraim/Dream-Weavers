using UnityEngine;

/// <summary>
/// 治疗抑制 - 受到的治疗效果降低
/// </summary>
public class HealingReductionDebuff : Buff
{
    private readonly float reductionPercent;

    public override string DisplayName => "治疗抑制";
    public override string Description => $"受到的治疗效果降低{(reductionPercent * 100):F0}%";

    public HealingReductionDebuff(IBattleUnit owner, int duration, float reduction)
        : base(owner, duration)
    {
        reductionPercent = Mathf.Clamp01(reduction);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log(
            $"{Owner?.DisplayName} afflicted with Healing Reduction for {RemainingTurns} turns"
        );
    }

    // 注意：此Debuff需要在治疗逻辑中检查并调用修改
    // 可以通过BattleModel提供方法来计算修改后的治疗量
    public int ModifyHealing(int baseHealing)
    {
        int reducedHealing = Mathf.RoundToInt(baseHealing * (1f - reductionPercent));
        Debug.Log($"{Owner?.DisplayName}'s healing reduced from {baseHealing} to {reducedHealing}");
        return Mathf.Max(0, reducedHealing);
    }
}
