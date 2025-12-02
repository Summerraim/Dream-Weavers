using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Sacrifice Heal")]
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
