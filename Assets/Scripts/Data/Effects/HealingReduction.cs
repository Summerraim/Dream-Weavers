using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Healing Reduction")]
// 治疗抑制：受到的治疗效果降低
public class HealingReduction : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float reductionPercent = 0.5f;

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
            Debug.LogWarning("HealingReduction: No active battle model found");
            return;
        }

        var debuff = new HealingReductionDebuff(receiver, duration, reductionPercent);
        CurrentBattle.AddBuff(debuff);
    }
}
