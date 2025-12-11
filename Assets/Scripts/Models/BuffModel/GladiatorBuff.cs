using UnityEngine;

/// <summary>
/// 角斗士Buff：连续登场6回合后触发"角斗"效果
/// 每回合开始时随机扣除敌方最大生命值10*X%（X∈1-6）
/// </summary>
public class GladiatorBuff : Buff
{
    public override string DisplayName => "角斗士";
    public override string Description =>
        isGladiatorMode
            ? $"角斗模式激活！已连续登场{consecutiveTurns}回合"
            : $"连续登场{consecutiveTurns}/6回合";

    private int consecutiveTurns; // 连续登场回合数
    private bool isGladiatorMode; // 是否已触发角斗模式
    private BattleModel battleModel;
    private IBattleUnit enemy;

    public GladiatorBuff(IBattleUnit owner, BattleModel battleModel, IBattleUnit enemy)
        : base(owner, -1) // 永久Buff
    {
        this.battleModel = battleModel;
        this.enemy = enemy;
        this.consecutiveTurns = 0;
        this.isGladiatorMode = false;
    }

    public override void OnTurnStart()
    {
        if (Owner == null || enemy == null)
            return;

        // 检查该Spirit是否是当前登场的Spirit
        bool isCurrentSpirit = battleModel.PlayerUnit == Owner;

        if (isCurrentSpirit)
        {
            // 增加连续登场计数
            consecutiveTurns++;

            Debug.Log(
                $"GladiatorBuff: {Owner.DisplayName} consecutive turns: {consecutiveTurns}"
            );

            // 达到6回合触发角斗模式
            if (consecutiveTurns >= 6)
            {
                if (!isGladiatorMode)
                {
                    isGladiatorMode = true;
                    Debug.Log($"GladiatorBuff: {Owner.DisplayName} entered Gladiator Mode!");
                }

                // 角斗效果：随机扣除敌方最大生命值10*X%
                int randomMultiplier = Random.Range(1, 7); // 1-6
                float damagePercent = 0.1f * randomMultiplier;
                int damage = Mathf.CeilToInt(enemy.MaxHP * damagePercent);

                Debug.Log(
                    $"GladiatorBuff: {Owner.DisplayName} deals {damage} damage ({damagePercent * 100}% of max HP) to {enemy.DisplayName} (ignoring immunity)"
                );

                // 直接扣除生命值（无视无敌/免疫）
                enemy.ReceiveDamage(damage);
            }
        }
        else
        {
            // 不是当前Spirit，重置计数
            if (consecutiveTurns > 0)
            {
                Debug.Log(
                    $"GladiatorBuff: {Owner.DisplayName} is no longer active, resetting consecutive turns"
                );
                consecutiveTurns = 0;
                isGladiatorMode = false;
            }
        }
    }

    public override void OnApplied()
    {
        Debug.Log($"GladiatorBuff: Applied to {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
