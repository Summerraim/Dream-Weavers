using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Base/Shield")]
// 护盾：为目标提供临时护盾，吸收一定伤害
public class Shield : Effect
{
    [SerializeField, Min(0)]
    private int initShield = 100;

    [SerializeField, Range(0f, 1f)]
    private float percentMaxHP = 0.2f;

    [SerializeField, Min(1)]
    private int duration = 3;

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
            Debug.LogWarning("Shield: No active battle model found");
            return;
        }

        int totalShield = Mathf.Max(0, initShield);
        if (percentMaxHP > 0f && receiver.MaxHP > 0)
        {
            totalShield += Mathf.CeilToInt(receiver.MaxHP * Mathf.Clamp01(percentMaxHP));
        }

        if (totalShield <= 0)
            return;

        var buff = new ShieldBuff(receiver, duration, totalShield, this);
        CurrentBattle.AddBuff(buff);
    }
}
