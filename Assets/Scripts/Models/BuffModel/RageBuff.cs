using UnityEngine;

/// <summary>
/// 怒意Buff：用于狂战士羁绊
/// 跟踪怒意层数，并提供攻击力加成
/// </summary>
public class RageBuff : Buff
{
    public override string DisplayName => "怒意";
    public override string Description =>
        $"每层增加{damagePerStack}点攻击力。当前层数：{currentStacks}";

    private int currentStacks;
    private int maxStacks;
    private int damagePerStack; // 每层怒意增加的攻击力
    private bool consumeOnHigherDamage; // 攻击力高于敌方时是否消耗怒意
    private BattleModel battleModel;

    /// <summary>
    /// 当前怒意层数
    /// </summary>
    public int CurrentStacks => currentStacks;

    public RageBuff(
        IBattleUnit owner,
        BattleModel battleModel,
        int damagePerStack,
        bool consumeOnHigherDamage,
        int maxStacks = 999
    )
        : base(owner, -1) // 永久Buff
    {
        this.battleModel = battleModel;
        this.damagePerStack = damagePerStack;
        this.consumeOnHigherDamage = consumeOnHigherDamage;
        this.maxStacks = maxStacks;
        this.currentStacks = 0;
    }

    /// <summary>
    /// 添加怒意层数
    /// </summary>
    public void AddStack(int amount = 1)
    {
        int oldStacks = currentStacks;
        currentStacks = Mathf.Min(currentStacks + amount, maxStacks);

        if (currentStacks > oldStacks)
        {
            Debug.Log(
                $"RageBuff: {Owner?.DisplayName} gained {currentStacks - oldStacks} rage stack(s). Total: {currentStacks}, Damage Bonus: {GetDamageBonus()}"
            );
        }
    }

    /// <summary>
    /// 消耗所有怒意层数并造成伤害
    /// </summary>
    /// <param name="target">目标单位</param>
    /// <returns>造成的伤害值</returns>
    public int ConsumeStacks(IBattleUnit target)
    {
        if (currentStacks == 0 || consumeOnHigherDamage == false)
            return 0;

        int damage = currentStacks * 10;
        Debug.Log(
            $"RageBuff: {Owner?.DisplayName} consumed {currentStacks} rage stacks, dealing {damage} damage to {target?.DisplayName}"
        );

        // 造成伤害
        target?.ReceiveDamage(damage);

        // 清空怒意
        currentStacks = 0;

        return damage;
    }

    /// <summary>
    /// 检查攻击力并决定是积累还是消耗怒意
    /// </summary>
    /// <param name="enemy">敌方单位</param>
    public void CheckAndApplyRage(IBattleUnit enemy)
    {
        if (Owner == null || enemy == null)
            return;

        // 获取基础攻击力
        int ownerBaseDamage = (Owner as Spirit)?.BaseDamage ?? 0;
        int enemyBaseDamage = (int)((enemy as Enemy)?.Damage ?? 0);

        Debug.Log(
            $"RageBuff: Checking rage - Owner base damage: {ownerBaseDamage}, Enemy base damage: {enemyBaseDamage}"
        );

        if (ownerBaseDamage < enemyBaseDamage)
        {
            // 基础攻击力低于敌方，积1层怒意
            AddStack(1);
        }
        else if (ownerBaseDamage > enemyBaseDamage && consumeOnHigherDamage)
        {
            // 基础攻击力高于敌方，消耗所有怒意
            ConsumeStacks(enemy);
        }
    }

    public override int GetDamageBonus()
    {
        return currentStacks * damagePerStack;
    }

    public override void OnApplied()
    {
        Debug.Log($"RageBuff: Applied to {Owner?.DisplayName}");
    }

    public override void OnRemoved()
    {
        Debug.Log($"RageBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 怒意是永久的，不会随回合消失
        // 不调用base.OnTurnEnd()以避免减少持续时间
    }
}
