using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Heal All (20%)")]
/// <summary>
/// 群体治疗 Effect：回复场上所有精灵 20% 生命值；
/// 并检查是否存在指定心兽，若存在则扣除其 30% 生命值。
/// </summary>
public class HealAll : Effect
{
    [SerializeField, Range(0f,1f)]
    private float healPercent = 0.20f;

    [SerializeField]
    private SpiritData penalizedSpirit; // 特定心兽（可为空）

    [SerializeField, Range(0f,1f)]
    private float penalizePercent = 0.30f;

    [SerializeField, Min(1)]
    private int dummyDuration = 1; // 一次性 Buff 的占位持续

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (CurrentBattle == null)
        {
            Debug.LogWarning("HealAll: No active battle model found");
            return;
        }

        var buff = new HealAllBuff(
            owner: caster,
            duration: dummyDuration,
            battleModel: CurrentBattle,
            healPercent: healPercent,
            penalizedSpirit: penalizedSpirit,
            penalizePercent: penalizePercent,
            sourceEffect: this
        );

        CurrentBattle.AddBuff(buff);
    }
}
