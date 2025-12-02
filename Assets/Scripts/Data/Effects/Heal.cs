using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Heal")]
public class Heal : Effect
{
    [SerializeField, Min(0)]
    private int flatHealing = 40;

    [SerializeField, Range(0f, 1f)]
    private float percentOfMaxHP = 0f;

    [SerializeField]
    private bool applyToCaster = false;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        int totalHealing = Mathf.Max(0, flatHealing);
        if (percentOfMaxHP > 0f && receiver.MaxHP > 0)
        {
            totalHealing += Mathf.CeilToInt(receiver.MaxHP * Mathf.Clamp01(percentOfMaxHP));
        }

        if (totalHealing <= 0)
            return;

        receiver.ReceiveHeal(totalHealing);
    }
}
