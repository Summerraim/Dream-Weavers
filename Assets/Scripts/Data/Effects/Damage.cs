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

        // 记录目标受伤前的HP，用于计算实际伤害
        int hpBefore = target.HP;

        target.ReceiveDamage(totalDamage, caster);

        // 计算实际造成的伤害（可能被护盾、防御等减免）
        int actualDamage = hpBefore - target.HP;

        // 通知BattleModel，触发施法者的OnDamageDealt（如吸血）
        if (caster != null && actualDamage > 0 && BattleModel.ActiveBattle != null)
        {
            BattleModel.ActiveBattle.NotifyDamageDealt(caster, actualDamage, target);
        }
    }
}
