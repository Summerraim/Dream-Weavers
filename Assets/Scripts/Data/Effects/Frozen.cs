using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/ControlDebuff/Frozen")]
// 冰冻束缚：使目标无法行动若干回合
public class Frozen : Effect
{
    [SerializeField, Min(1)]
    private int duration = 2;

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
            Debug.LogWarning("Frozen: No active battle model found");
            return;
        }

        var debuff = new FrozenDebuff(receiver, duration, this);
        CurrentBattle.AddBuff(debuff);
    }
}
