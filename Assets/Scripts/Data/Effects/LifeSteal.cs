using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Life Steal")]
/*生命偷取
总伤害 = flatDamage + (施法者攻击力 × casterDamageMultiplier)
实际造成伤害 = 目标受伤前HP - 目标受伤后HP
治疗量 = 实际造成伤害 × stealPercent
*/
public class LifeSteal : Effect
{
    [SerializeField, Min(0)]
    private int initDamage = 30;

    [SerializeField, Min(0f)]
    private float casterDamageMultiplier = 1f;

    [SerializeField, Range(0f, 1f)]
    private float stealPercent = 0.5f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        int totalDamage = initDamage;
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

        // 通知BattleModel伤害已造成（触发OnDamageDealt，如Vampiric buff）
        if (caster != null && dealt > 0 && BattleModel.ActiveBattle != null)
        {
            BattleModel.ActiveBattle.NotifyDamageDealt(caster, dealt, target);
        }

        // LifeSteal技能自带的吸血效果
        if (caster != null && stealPercent > 0f && dealt > 0)
        {
            int heal = Mathf.CeilToInt(dealt * stealPercent);
            caster.ReceiveHeal(heal);
        }
    }
}
