using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Critical Strike")]
// 暴击强化：提高攻击力，并有几率造成额外伤害
public class CriticalStrike : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float damageBonus = 0.3f; // 攻击力提升30%

    [SerializeField, Range(0f, 1f)]
    private float critChance = 0.3f; // 30%暴击几率

    [SerializeField, Range(1f, 3f)]
    private float critMultiplier = 2f; // 暴击伤害倍率

    [SerializeField, Min(1)]
    private int duration = 3;

    [SerializeField]
    private bool applyToCaster = true;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        if (BattleModel.ActiveBattle == null)
        {
            Debug.LogWarning("CriticalStrike: No active battle model found");
            return;
        }

        var buff = new CriticalStrikeBuff(
            receiver,
            duration,
            damageBonus,
            critChance,
            critMultiplier,
            this
        );
        BattleModel.ActiveBattle.AddBuff(buff);
    }
}
