using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Weaken")]
//虚弱：伤害 = 目标最大HP × value
public class Weaken : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float value = 0.2f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        float clampedPercent = Mathf.Clamp01(value);
        int damage = Mathf.CeilToInt(target.MaxHP * clampedPercent);
        target.ReceiveDamage(damage);
    }
}
