using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Revive")]
// 复活：死亡后立即复活并恢复一定比例生命值（一次性）
public class Revive : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float reviveHealthPercent = 0.3f;

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
            Debug.LogWarning("ApplyReviveBuff: No active battle model found");
            return;
        }

        var buff = new ReviveBuff(receiver, reviveHealthPercent, this);
        CurrentBattle.AddBuff(buff);
    }
}
