using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Mana Leech")]
// 能量流失：每回合损失魔法值
public class ManaLeech : Effect
{
    [SerializeField]
    private bool usePercentLoss = true;

    [SerializeField, Range(0f, 1f)]
    private float percentLoss = 0.1f;

    [SerializeField, Min(0)]
    private int flatLoss = 20;

    [SerializeField, Min(1)]
    private int duration = 3;

    [SerializeField]
    private bool applyToTarget = true;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToTarget ? target : caster;
        if (receiver == null)
            return;

        if (BattleModel.ActiveBattle == null)
        {
            Debug.LogWarning("ManaLeech: No active battle model found");
            return;
        }

        Buff debuff;
        if (usePercentLoss)
        {
            debuff = new ManaLeechDebuff(receiver, duration, percentLoss, this);
        }
        else
        {
            debuff = new ManaLeechDebuff(receiver, duration, flatLoss, this);
        }

        BattleModel.ActiveBattle.AddBuff(debuff);
    }
}
