using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Strengthen")]
// 力量祝福：攻击力提升30%，持续3回合
public class Strengthen : Effect
{
    [SerializeField, Range(0f, 2f)]
    private float damageMultiplier = 0.3f;

    [SerializeField, Min(1)]
    private int duration = 3;

    [SerializeField]
    private bool applyToCaster = true;

    // 静态引用到当前的BattleModel（临时解决方案）
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("ApplyStrengthBuff: No active battle model found");
            return;
        }

        var buff = new StrengthBuff(receiver, duration, damageMultiplier, this);
        CurrentBattle.AddBuff(buff);
    }
}
