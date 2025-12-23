using UnityEngine;

/// <summary>
/// 睡眠花粉 - 沉睡若干回合，受到攻击立即醒来
/// </summary>
public class SleepDebuff : Buff
{
    public override string DisplayName => "睡眠";
    public override string Description => "陷入沉睡，无法行动，受到攻击会醒来";
    public override bool IsStackable => true; // 允许叠加持续时间

    private BattleModel battleModel;

    public SleepDebuff(IBattleUnit owner, int duration, BattleModel model, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        battleModel = model;
    }

    public override void OnApplied()
    {
        Debug.Log($"{Owner?.DisplayName} 陷入了沉睡！将沉睡 {RemainingTurns} 回合");
    }

    public override void OnRemoved()
    {
        Debug.Log($"{Owner?.DisplayName} 从睡眠中醒来了");
    }

    public override void OnDamageReceived(int actualDamage, IBattleUnit attacker)
    {
        // 受到任何伤害都会醒来
        if (actualDamage > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 受到攻击，从睡眠中醒来！");
            // 立即移除此Debuff
            if (battleModel != null)
            {
                battleModel.RemoveBuff(this);
            }
        }
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        if (RemainingTurns > 0)
        {
            Debug.Log($"{Owner?.DisplayName} 仍在沉睡，剩余 {RemainingTurns} 回合");
        }
    }
}
