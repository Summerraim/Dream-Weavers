using UnityEngine;

/// <summary>
/// 燃法者Buff：造成伤害时额外扣除敌方法力值
/// </summary>
public class ManaBurnBuff : Buff
{
    public override string DisplayName => "燃法者";
    public override string Description => $"造成伤害时额外扣除对方该次伤害{burnPercent * 100}%的最大法力值";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float burnPercent; // 扣除百分比（0.1 = 10%, 0.2 = 20%）

    public ManaBurnBuff(IBattleUnit owner, float burnPercent)
        : base(owner, -1) // 永久Buff
    {
        this.burnPercent = burnPercent;
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        if (target == null || actualDamage <= 0)
            return;

        // 计算要扣除的法力值：该次伤害的X%，以目标最大法力值为基准
        int manaDrain = Mathf.CeilToInt(actualDamage * burnPercent);

        // 限制不超过目标当前法力值
        manaDrain = Mathf.Min(manaDrain, target.Mana);

        if (manaDrain > 0)
        {
            // 扣除法力值
            target.ConsumeMana(manaDrain);

            Debug.Log(
                $"ManaBurnBuff: {Owner?.DisplayName} burns {manaDrain} mana from {target.DisplayName} ({burnPercent * 100}% of {actualDamage} damage)"
            );
        }
    }

    public override void OnApplied()
    {
        Debug.Log($"ManaBurnBuff: Applied to {Owner?.DisplayName}, burn rate: {burnPercent * 100}%");
    }

    public override void OnRemoved()
    {
        Debug.Log($"ManaBurnBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
