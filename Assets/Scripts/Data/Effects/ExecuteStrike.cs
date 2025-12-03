using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Execute Strike")]
public class ExecuteStrike : Effect
{
    [SerializeField, Min(0)]
    private int flatDamage = 60;

    [SerializeField, Range(0f, 1f)]
    private float missingHealthScaling = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float executeThreshold = 0.25f;

    [SerializeField, Min(1f)]
    private float executeMultiplier = 2f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int totalDamage = flatDamage;
        if (missingHealthScaling > 0f && target.MaxHP > 0)
        {
            float missingRatio = 1f - Mathf.Clamp01((float)target.HP / target.MaxHP);
            totalDamage += Mathf.RoundToInt(target.MaxHP * missingRatio * missingHealthScaling);

            if (missingRatio >= executeThreshold)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * executeMultiplier);
            }
        }

        totalDamage = Mathf.Max(0, totalDamage);
        if (totalDamage == 0)
            return;

        target.ReceiveDamage(totalDamage);
    }
}
