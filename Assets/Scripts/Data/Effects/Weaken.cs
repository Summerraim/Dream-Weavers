using UnityEngine;

/// <summary>
/// 虚弱效果：降低目标的攻击力和防御力
/// 应用后会给目标添加WeakenBuff，持续数回合
/// </summary>
[CreateAssetMenu(menuName = "Data/Effects/Debuff/Weaken")]
public class Weaken : Effect
{
    [Header("虚弱配置")]
    [SerializeField, Tooltip("Debuff持续回合数")]
    private int duration = 3;

    [SerializeField, Range(0f, 1f), Tooltip("造成伤害降低百分比 (0.2 = 20%)")]
    private float damageReduction = 0.2f;

    [SerializeField, Range(0f, 1f), Tooltip("护甲降低百分比 (0.2 = 20%)")]
    private float defenseReduction = 0.2f;

    [SerializeField, Tooltip("应用到施放者而非目标")]
    private bool applyToCaster = false;

    /// <summary>
    /// 静态引用到当前战斗模型，用于添加Buff
    /// 需要在战斗开始时由BattleController设置
    /// </summary>
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToCaster ? caster : target;
        if (receiver == null)
        {
            Debug.LogWarning("Weaken: Target is null");
            return;
        }

        if (CurrentBattle == null)
        {
            Debug.LogWarning("Weaken: No active battle model found");
            return;
        }

        // 创建并添加虚弱Debuff（传递this作为SourceEffect）
        var weakenBuff = new WeakenBuff(receiver, duration, damageReduction, defenseReduction, this);
        CurrentBattle.AddBuff(weakenBuff);

        Debug.Log(
            $"Weaken: Applied to {receiver.DisplayName} " +
            $"(Duration: {duration}, DamageReduction: {damageReduction * 100}%, " +
            $"DefenseReduction: {defenseReduction * 100}%)"
        );
    }
}
