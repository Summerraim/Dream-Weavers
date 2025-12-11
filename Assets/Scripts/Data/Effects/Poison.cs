using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Poison")]
// 中毒：每回合损失一定生命值
public class Poison : Effect
{
    [SerializeField]
    private bool usePercentDamage = false;

    [SerializeField, Range(0f, 0.5f)]
    private float percentDamage = 0.1f;

    [SerializeField, Min(0)]
    private int initDamage = 50;

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
            Debug.LogWarning("Poison: No active battle model found");
            return;
        }

        Buff debuff;
        if (usePercentDamage)
        {
            debuff = new PoisonDebuff(receiver, duration, percentDamage, this);
        }
        else
        {
            debuff = new PoisonDebuff(receiver, duration, initDamage, this);
        }

        CurrentBattle.AddBuff(debuff);
    }
}
