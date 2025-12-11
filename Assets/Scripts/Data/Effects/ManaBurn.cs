using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Mana Burn")]
// 法力燃烧：造成伤害并消耗目标法力，消耗的法力可转化为额外伤害
public class ManaBurn : Effect
{
    [SerializeField, Min(0)]
    private int initDamage = 50;

    [SerializeField, Range(0f, 1f)]
    private float manaBurnPercent = 0.3f; // 消耗目标30%当前法力

    [SerializeField, Range(0f, 2f)]
    private float manaToExtraDamageRatio = 0.5f; // 每消耗1点法力转化为0.5点额外伤害

    [SerializeField]
    private bool scaleWithCasterDamage = true;

    [SerializeField, Min(0f)]
    private float damageMultiplier = 0.8f;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (target == null)
            return;

        // 计算基础伤害
        int totalDamage = initDamage;
        if (scaleWithCasterDamage && caster != null)
        {
            totalDamage += Mathf.RoundToInt(caster.Damage * damageMultiplier);
        }

        // 燃烧法力
        int manaBurned = 0;
        if (target.Mana > 0 && manaBurnPercent > 0f)
        {
            manaBurned = Mathf.CeilToInt(target.Mana * manaBurnPercent);
            target.ConsumeMana(manaBurned);
            Debug.Log($"{target.DisplayName} 被燃烧了 {manaBurned} 点法力！");
        }

        // 法力转化为额外伤害
        if (manaBurned > 0 && manaToExtraDamageRatio > 0f)
        {
            int extraDamage = Mathf.RoundToInt(manaBurned * manaToExtraDamageRatio);
            totalDamage += extraDamage;
            Debug.Log($"法力燃烧产生了 {extraDamage} 点额外伤害！");
        }

        totalDamage = Mathf.Max(0, totalDamage);
        if (totalDamage > 0)
        {
            target.ReceiveDamage(totalDamage);
        }
    }
}
