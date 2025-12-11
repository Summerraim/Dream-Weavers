using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Damage")]
//基础伤害：总伤害 = 固定伤害 + (施法者攻击力 × 伤害倍率)
public class Damage : Effect
{
    [SerializeField, Min(0)]
    private int initDamage = 0;

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
