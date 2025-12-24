using UnityEngine;

/// <summary>
/// 虚空遗民Buff：攻击和技能无视敌人部分防御力
/// 注意：这个Buff需要配合BattleModel的伤害计算系统使用
/// 可以通过设置一个标记属性来实现无视防御的效果
/// </summary>
public class VoidExileBuff : Buff
{
    public override string DisplayName => "虚空遗民";
    public override string Description =>
        $"攻击和技能无视敌人{ignoreDefensePercent * 100}%的防御力";

    // Synergy Buff不在UI中显示
    public override bool ShowInUI => false;

    private float ignoreDefensePercent;

    public float IgnoreDefensePercent => ignoreDefensePercent;

    public VoidExileBuff(IBattleUnit owner, float ignoreDefensePercent)
        : base(owner, -1) // 永久Buff
    {
        this.ignoreDefensePercent = ignoreDefensePercent;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"VoidExileBuff: Applied to {Owner?.DisplayName}, ignoring {ignoreDefensePercent * 100}% of enemy defense"
        );
    }

    public override void OnRemoved()
    {
        Debug.Log($"VoidExileBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
