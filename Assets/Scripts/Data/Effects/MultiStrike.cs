using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Multi Strike")]
public class MultiStrike : Effect
{
    [SerializeField, Range(1, 10)]
    private int strikes = 3;

    [SerializeField, Min(0)]
    private int damagePerStrike = 15;

    [SerializeField]
    private bool scaleWithCasterDamage = false;

    [SerializeField, Min(0f)]
    private float casterDamageMultiplier = 0.3f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int baseStrike = damagePerStrike;
        if (scaleWithCasterDamage && caster != null && casterDamageMultiplier > 0f)
        {
            baseStrike += Mathf.RoundToInt(caster.Damage * casterDamageMultiplier);
        }

        baseStrike = Mathf.Max(0, baseStrike);
        if (baseStrike == 0)
            return;

        for (int i = 0; i < Mathf.Max(1, strikes); i++)
        {
            target.ReceiveDamage(baseStrike);
            if (target.IsDead)
                break;
        }
    }
}
