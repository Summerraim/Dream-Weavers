using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Heal")]
//治疗:总治疗 = flatHealing + (接受者最大HP × percentMaxHP)
//如果 applyToCaster 为 true，治疗施法者；否则治疗目标
public class Heal : Effect
{
    [SerializeField, Min(0)]
    private int initHealing = 40;

    [SerializeField, Range(0f, 1f)]
    private float percentMaxHP = 0.1f;

    [SerializeField]
    private bool applyToCaster = false;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        int totalHealing = Mathf.Max(0, initHealing);
        if (percentMaxHP > 0f && receiver.MaxHP > 0)
        {
            totalHealing += Mathf.CeilToInt(receiver.MaxHP * Mathf.Clamp01(percentMaxHP));
        }

        if (totalHealing <= 0)
            return;

        receiver.ReceiveHeal(totalHealing);
    }
}
