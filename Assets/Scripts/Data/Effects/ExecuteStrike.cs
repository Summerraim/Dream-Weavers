using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Execute Strike")]
/*斩杀打击
基础伤害 = initDamage + (目标最大血量 × 已损失血量比例 × missingHealthScaling)
如果 已损失血量比例 ≥ executeThreshold:
    最终伤害 = 基础伤害 × executeMultiplier
否则:
    最终伤害 = 基础伤害
*/
public class ExecuteStrike : Effect
{
    [SerializeField, Min(0)]
    private int initDamage = 60;

    [SerializeField, Range(0f, 1f)]
    private float missingHealthScaling = 0.1f;

    [SerializeField, Range(0f, 1f)]
    private float executeThreshold = 0.75f;

    [SerializeField, Min(1f)]
    private float executeMultiplier = 2f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int totalDamage = initDamage;
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
