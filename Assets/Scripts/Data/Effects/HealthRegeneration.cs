using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Health Regeneration")]
// 生命源泉：每回合恢复10%最大生命值
public class HealthRegeneration : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float percentRegeneration = 0.1f;

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
            Debug.LogWarning("ApplyHealthRegenerationBuff: No active battle model found");
            return;
        }

        var buff = new HealthRegenerationBuff(receiver, duration, percentRegeneration, this);
        CurrentBattle.AddBuff(buff);
    }
}
