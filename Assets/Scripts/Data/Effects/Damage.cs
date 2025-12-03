using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Damage")]
public class Damage : Effect
{
    [SerializeField, Min(0)]
    private int initDamage = 10;

    [SerializeField]
    private bool scaleWithDamage = true;

    [SerializeField, Min(0f)]
    private float DamageMultiplier = 1f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int totalDamage = initDamage;

        if (scaleWithDamage && caster != null)
        {
            totalDamage += Mathf.RoundToInt(caster.Damage * DamageMultiplier);
        }

        totalDamage = Mathf.Max(0, totalDamage);
        if (totalDamage == 0)
            return;

        target.ReceiveDamage(totalDamage);
    }
}
