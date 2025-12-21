using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/ControlDebuff/Sleep")]
// 睡眠花粉：使目标沉睡若干回合，受到攻击立即醒来
public class Sleep : Effect
{
    [SerializeField, Range(1, 3)]
    private int minDuration = 1;

    [SerializeField, Range(1, 3)]
    private int maxDuration = 3;

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
            Debug.LogWarning("Sleep: No active battle model found");
            return;
        }

        if (!TryTrigger(receiver))
            return;

        // 随机持续时间
        int duration = Random.Range(minDuration, maxDuration + 1);
        var debuff = new SleepDebuff(receiver, duration, CurrentBattle, this); // 传递 this (Sleep Effect) 作为 sourceEffect
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
