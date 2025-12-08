using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/ManaShield")]
// 能量护甲：将一部分攻击伤害转化为魔法消耗
public class ManaShield : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float absorptionPercent = 0.5f;

    [SerializeField, Range(0.1f, 2f)]
    private float damageToManaRatio = 1.0f;

    [SerializeField, Min(1)]
    private int duration = 999; // 默认持久

    [SerializeField]
    private bool applyToCaster = true;

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("ApplyManaShieldBuff: No active battle model found");
            return;
        }

        var buff = new ManaShieldBuff(receiver, duration, absorptionPercent, damageToManaRatio);
        CurrentBattle.AddBuff(buff);
    }
}
