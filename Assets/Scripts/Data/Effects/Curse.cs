using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Curse")]
// 诅咒：降低最大生命值上限
public class Curse : Effect
{
    [SerializeField, Range(0f, 0.5f)]
    private float maxHPReduction = 0.3f; // 最大生命值降低30%

    [SerializeField, Min(1)]
    private int duration = 3;

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
            Debug.LogWarning("Curse: No active battle model found");
            return;
        }

        var debuff = new CurseDebuff(receiver, duration, maxHPReduction);
        CurrentBattle.AddBuff(debuff);
    }
}
