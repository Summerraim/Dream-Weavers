using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/ControlDebuff/Frozen")]
// 冰冻束缚：使目标无法行动若干回合
public class Frozen : Effect
{
    [SerializeField, Min(1)]
    private int duration = 2;

    [SerializeField, Range(0f, 1f)]
    private float triggerChance = 1f;

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

        if (!TryTrigger(receiver))
            return;

        var debuff = new FrozenDebuff(receiver, duration, this);
        CurrentBattle.AddBuff(debuff);
    }

    private bool TryTrigger(IBattleUnit receiver)
    {
        float roll = Random.value;
        float chance = Mathf.Clamp01(triggerChance);
        bool triggered = roll <= chance;

        if (!triggered)
        {
            string targetName = receiver?.DisplayName ?? "目标";
            Debug.Log($"{DisplayName} 未能对 {targetName} 生效（判定 {roll:F2} / 需要 {chance:F2}）");
        }

        return triggered;
    }
}
