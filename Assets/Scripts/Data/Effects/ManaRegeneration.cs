using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Mana Regeneration")]
// 能量充沛：每回合恢复10%最大魔法值，持续3回合
public class ManaRegeneration : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float percentRegeneration = 0.1f;

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
            Debug.LogWarning("ApplyManaRegenerationBuff: No active battle model found");
            return;
        }

        var buff = new ManaRegenerationBuff(receiver, duration, percentRegeneration, this);
        BattleModel.ActiveBattle.AddBuff(buff);
    }
}
