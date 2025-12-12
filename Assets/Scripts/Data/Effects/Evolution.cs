using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Evolution")]
/// <summary>
/// 精灵进化/替换 Effect：应用后使目标（通常为施法者/玩家单位）替换为指定 SpiritData。
/// </summary>
public class Evolution : Effect
{
    [SerializeField]
    private SpiritData targetSpirit;

    [SerializeField]
    private bool applyToCaster = true; // 为 true 时对施法者生效，否则对目标生效

    [SerializeField, Min(1)]
    private int dummyDuration = 1; // 兼容 Buff 构造，进化为一次性效果

    // 与其它 Effect 保持一致：由外部在战斗开始或切换时注入
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (CurrentBattle == null)
        {
            Debug.LogWarning("Evolution: No active battle model found");
            return;
        }
        if (targetSpirit == null)
        {
            Debug.LogWarning("Evolution: targetSpirit is null");
            return;
        }

        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
        {
            Debug.LogWarning("Evolution: receiver is null");
            return;
        }

        var buff = new EvolutionBuff(receiver, dummyDuration, CurrentBattle, targetSpirit, this);
        CurrentBattle.AddBuff(buff);
    }
}
