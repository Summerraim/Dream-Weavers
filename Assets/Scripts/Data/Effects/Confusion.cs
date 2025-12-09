using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/ControlDebuff/Confusion")]
// 混乱：有几率攻击自己而非敌人
public class Confusion : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float confusionChance = 0.5f; // 50%几率混乱

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
            Debug.LogWarning("Confusion: No active battle model found");
            return;
        }

        var debuff = new ConfusionDebuff(receiver, duration, confusionChance);
        CurrentBattle.AddBuff(debuff);
    }
}
