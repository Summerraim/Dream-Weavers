using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Burn")]
// 燃烧：每回合造成火焰伤害
public class Burn : Effect
{
    [Header("伤害设置")]
    [SerializeField]
    private bool usePercentDamage = false;

    [SerializeField, Range(0f, 0.3f)]
    private float percentDamage = 0.08f; // 8%最大生命值

    [SerializeField, Min(0)]
    private int initDamage = 40;

    [Header("持续时间")]
    [SerializeField, Min(1)]
    private int duration = 3;

    [Header("目标")]
    [SerializeField]
    private bool applyToTarget = true;

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToTarget ? target : caster;
        if (receiver == null)
            return;

        if (CurrentBattle == null)
        {
            Debug.LogWarning("Burn: No active battle model found");
            return;
        }

        var debuff = new BurnDebuff(
            receiver,
            duration,
            initDamage,
            percentDamage,
            usePercentDamage
        );
        CurrentBattle.AddBuff(debuff);
    }
}
