using UnityEngine;

/// <summary>
/// 战斗中的持续效果（Buff/Debuff）
/// </summary>
public abstract class Buff
{
    /// <summary>
    /// Buff的显示名称
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Buff的描述
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 剩余持续回合数（-1表示永久）
    /// </summary>
    public int RemainingTurns { get; private set; }

    /// <summary>
    /// Buff的拥有者
    /// </summary>
    public IBattleUnit Owner { get; private set; }

    /// <summary>
    /// Buff是否已过期
    /// </summary>
    public bool IsExpired => RemainingTurns == 0;

    /// <summary>
    /// 是否为一次性Buff（触发后立即移除）
    /// </summary>
    public bool IsOneTime { get; protected set; }

    /// <summary>
    /// 一次性Buff是否已触发
    /// </summary>
    public bool HasTriggered { get; protected set; }

    protected Buff(IBattleUnit owner, int duration)
    {
        Owner = owner;
        RemainingTurns = duration;
        IsOneTime = false;
        HasTriggered = false;
    }

    /// <summary>
    /// Buff被应用时调用（首次添加时）
    /// </summary>
    public virtual void OnApplied()
    {
    }

    /// <summary>
    /// 每回合开始时调用
    /// </summary>
    public virtual void OnTurnStart()
    {
    }

    /// <summary>
    /// 每回合结束时调用，并减少持续时间
    /// </summary>
    public virtual void OnTurnEnd()
    {
        if (RemainingTurns > 0)
        {
            RemainingTurns--;
        }
    }

    /// <summary>
    /// Buff被移除时调用
    /// </summary>
    public virtual void OnRemoved()
    {
    }

    /// <summary>
    /// 修改伤害（在造成伤害时调用）
    /// </summary>
    public virtual int ModifyDamageDealt(int baseDamage)
    {
        return baseDamage;
    }

    /// <summary>
    /// 修改受到的伤害（在接受伤害时调用）
    /// </summary>
    public virtual int ModifyDamageReceived(int baseDamage)
    {
        return baseDamage;
    }

    /// <summary>
    /// 造成伤害后触发（用于吸血等效果）
    /// </summary>
    /// <param name="actualDamage">实际造成的伤害</param>
    /// <param name="target">受伤目标</param>
    public virtual void OnDamageDealt(int actualDamage, IBattleUnit target)
    {
    }

    /// <summary>
    /// 受到伤害后触发（用于反伤等效果）
    /// </summary>
    /// <param name="actualDamage">实际受到的伤害</param>
    /// <param name="attacker">攻击者</param>
    public virtual void OnDamageReceived(int actualDamage, IBattleUnit attacker)
    {
    }

    /// <summary>
    /// 单位即将死亡时触发（用于复活等效果）
    /// </summary>
    /// <returns>是否阻止死亡</returns>
    public virtual bool OnDeath()
    {
        return false;
    }

    /// <summary>
    /// 获取攻击力加成
    /// </summary>
    public virtual int GetDamageBonus()
    {
        return 0;
    }

    /// <summary>
    /// 获取防御力加成
    /// </summary>
    public virtual int GetDefenseBonus()
    {
        return 0;
    }
}
