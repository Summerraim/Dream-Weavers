using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Weaken")]
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
