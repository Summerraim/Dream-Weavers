using UnityEngine;

/// <summary>
/// 城堡卫队Buff：战斗开始时为自身提供护盾
/// </summary>
public class CastleGuardBuff : Buff
{
    public override string DisplayName => "城堡卫队";
    public override string Description =>
        $"战斗开始时提供{shieldPercent * 100}%攻击力的护盾";

    private float shieldPercent;
    private BattleModel battleModel;
    private bool hasApplied;

    public CastleGuardBuff(
        IBattleUnit owner,
        float shieldPercent,
        BattleModel battleModel
    )
        : base(owner, -1) // 永久Buff
    {
        this.shieldPercent = shieldPercent;
        this.battleModel = battleModel;
        this.hasApplied = false;
    }

    public override void OnApplied()
    {
        if (hasApplied || Owner == null || battleModel == null)
            return;

        hasApplied = true;

        int baseDamage = (Owner as Spirit)?.BaseDamage ?? 0;
        int shieldAmount = Mathf.CeilToInt(baseDamage * shieldPercent);

        // 为自身提供护盾
        var shieldBuff = new ShieldBuff(Owner, -1, shieldAmount); // 永久护盾
        battleModel.AddBuff(shieldBuff);

        Debug.Log($"CastleGuardBuff: Applied {shieldAmount} shield to {Owner.DisplayName}");
    }

    public override void OnRemoved()
    {
        Debug.Log($"CastleGuardBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
