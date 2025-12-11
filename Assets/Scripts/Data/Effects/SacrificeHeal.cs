using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Sacrifice Heal")]
/*献祭治疗
献祭HP = 施法者最大HP × healthSacrificePercent
实际献祭 = 施法者受伤前HP - 施法者受伤后HP
治疗量 = 实际献祭 × healMultiplier
*/
public class SacrificeHeal : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float healthSacrificePercent = 0.2f;

    [SerializeField, Range(0f, 2f)]
    private float healMultiplier = 1.5f;

    [SerializeField]
    private bool healCasterInstead = false;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (caster == null)
            return;

        IBattleUnit healTarget = healCasterInstead ? caster : target;
        if (healTarget == null)
            return;

        int sacrifice = Mathf.CeilToInt(caster.MaxHP * Mathf.Clamp01(healthSacrificePercent));
        if (sacrifice <= 0)
            return;

        int casterHpBefore = caster.HP;
        caster.ReceiveDamage(sacrifice);
        int actualSacrifice = Mathf.Max(0, casterHpBefore - caster.HP);
        if (actualSacrifice <= 0)
            return;

        int healAmount = Mathf.CeilToInt(actualSacrifice * healMultiplier);
        if (healAmount <= 0)
            return;

        healTarget.ReceiveHeal(healAmount);
    }
}
