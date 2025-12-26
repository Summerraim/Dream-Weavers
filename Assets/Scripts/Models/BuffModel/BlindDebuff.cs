using UnityEngine;

/// <summary>
/// 致盲 - 有概率使技能失效
/// </summary>
public class BlindDebuff : Buff
{
    private readonly float missChance;

    public override string DisplayName => "致盲";
    public override string Description => $"技能有{(missChance * 100):F0}%概率失效";
    public override bool IsStackable => true; // 允许叠加持续时间

    public BlindDebuff(IBattleUnit owner, int duration, float chance, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        missChance = Mathf.Clamp01(chance);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log(
            $"{Owner?.DisplayName} is blinded for {RemainingTurns} turns (miss chance: {(missChance * 100):F0}%)"
        );
    }

    /// <summary>
    /// 检查技能是否命中
    /// </summary>
    /// <returns>true表示命中，false表示未命中</returns>
    public bool CheckHit()
    {
        float roll = Random.Range(0f, 1f);
        bool hit = roll >= missChance;

        if (!hit)
        {
            Debug.Log(
                $"{Owner?.DisplayName} missed due to Blind! (rolled {roll:F2}, needed {missChance:F2})"
            );
        }

        return hit;
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        Debug.Log($"{Owner?.DisplayName} is no longer blinded");
    }
}
