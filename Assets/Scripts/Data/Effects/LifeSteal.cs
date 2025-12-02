using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Life Steal")]
public class LifeSteal : Effect
{
    [SerializeField, Min(0)]
    private int flatDamage = 30;

    [SerializeField, Min(0f)]
    private float casterDamageMultiplier = 1f;

    [SerializeField, Range(0f, 1f)]
    private float stealPercent = 0.5f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int totalDamage = flatDamage;
        if (caster != null && casterDamageMultiplier > 0f)
        {
            totalDamage += Mathf.RoundToInt(caster.Damage * casterDamageMultiplier);
        }

        totalDamage = Mathf.Max(0, totalDamage);
        if (totalDamage == 0)
            return;

        int targetHpBefore = target.HP;
        target.ReceiveDamage(totalDamage);
        int dealt = Mathf.Max(0, targetHpBefore - target.HP);

        if (caster != null && stealPercent > 0f && dealt > 0)
        {
            int heal = Mathf.CeilToInt(dealt * stealPercent);
            caster.ReceiveHeal(heal);
        }
    }
}
