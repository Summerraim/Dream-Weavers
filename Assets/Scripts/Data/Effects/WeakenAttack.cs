using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Weaken Attack")]
// 攻击降低：物理攻击力降低
public class WeakenAttack : Effect
{
    [SerializeField]
    private bool usePercentReduction = true;

    [SerializeField, Range(0f, 1f)]
    private float reductionPercent = 0.3f;

    [SerializeField, Min(0)]
    private int initReduction = 20;

    [SerializeField, Min(1)]
    private int duration = 3;

    [SerializeField]
    private bool applyToTarget = true;

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToTarget ? target : caster;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("WeakenAttack: No active battle model found");
            return;
        }

        Buff debuff;
        if (usePercentReduction)
        {
            debuff = new WeakenAttackDebuff(receiver, duration, reductionPercent);
        }
        else
        {
            debuff = new WeakenAttackDebuff(receiver, duration, initReduction);
        }

        CurrentBattle.AddBuff(debuff);
    }
}
