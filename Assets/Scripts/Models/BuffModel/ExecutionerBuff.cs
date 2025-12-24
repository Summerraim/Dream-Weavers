using UnityEngine;

/// <summary>
/// 处决者Buff：伤害结算时，若敌方生命值低于阈值，造成额外攻击力伤害
/// </summary>
public class ExecutionerBuff : Buff
{
    public override string DisplayName => "处决者";
    public override string Description =>
        $"敌方生命值低于{executeThreshold * 100}%时造成额外攻击力伤害";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float executeThreshold; // 触发阈值（0.1 = 10%, 0.2 = 20%）
    private BattleModel battleModel;

    public ExecutionerBuff(IBattleUnit owner, BattleModel battleModel, float executeThreshold)
        : base(owner, -1) // 永久Buff
    {
        this.battleModel = battleModel;
        this.executeThreshold = executeThreshold;
    }

    public override void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
        if (target == null || Owner == null)
            return;

        // 检查目标生命值百分比
        float hpPercent = (float)target.HP / target.MaxHP;

        if (hpPercent <= executeThreshold)
        {
            // 获取登场宠物的攻击力
            int ownerBaseDamage = (Owner as Spirit)?.BaseDamage ?? 0;

            if (ownerBaseDamage > 0)
            {
                Debug.Log(
                    $"ExecutionerBuff: {Owner.DisplayName} executes {target.DisplayName} (HP: {hpPercent * 100:F1}% <= {executeThreshold * 100}%), dealing extra {ownerBaseDamage} damage"
                );

                // 造成额外攻击力伤害
                target.ReceiveDamage(ownerBaseDamage);
            }
        }
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"ExecutionerBuff: Applied to {Owner?.DisplayName}, threshold={executeThreshold * 100}%"
        );
    }

    public override void OnRemoved()
    {
        Debug.Log($"ExecutionerBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff，不减少持续时间
    }
}
