using UnityEngine;

/// <summary>
/// 坚韧皮肤 - 减少受到的伤害
/// </summary>
public class ToughSkinBuff : Buff
{
    private readonly float damageReduction;

    public override string DisplayName => "坚韧皮肤";
    public override string Description => $"受到伤害减少{(damageReduction * 100):F0}%";

    public ToughSkinBuff(IBattleUnit owner, int duration, float reduction)
        : base(owner, duration)
    {
        damageReduction = Mathf.Clamp01(reduction);
    }

    public override void OnApplied()
    {
        base.OnApplied();
        Debug.Log($"{Owner?.DisplayName} gained Tough Skin: {(damageReduction * 100):F0}% damage reduction for {RemainingTurns} turns");
    }

    public override int ModifyDamageReceived(int baseDamage)
    {
        int reduced = Mathf.RoundToInt(baseDamage * (1f - damageReduction));
        return Mathf.Max(0, reduced);
    }
}
