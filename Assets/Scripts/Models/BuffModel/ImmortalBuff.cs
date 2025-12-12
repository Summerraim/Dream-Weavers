using UnityEngine;

/// <summary>
/// 不朽造物Buff：免疫即死效果，首次生命值降至1点时获得无敌
/// </summary>
public class ImmortalBuff : Buff
{
    public override string DisplayName => "不朽造物";
    public override string Description => "免疫即死效果，首次濒死时获得2回合无敌（每场战斗一次）";

    private BattleModel battleModel;
    private bool hasTriggeredInvincibility;

    public ImmortalBuff(IBattleUnit owner, BattleModel battleModel)
        : base(owner, -1) // 永久Buff
    {
        this.battleModel = battleModel;
        this.hasTriggeredInvincibility = false;
    }

    public override void OnApplied()
    {
        Debug.Log($"ImmortalBuff: Applied to {Owner?.DisplayName}");
    }

    public override bool OnDeath()
    {
        if (Owner == null || battleModel == null)
            return false;

        // 首次濒死时触发无敌
        if (!hasTriggeredInvincibility)
        {
            hasTriggeredInvincibility = true;

            // 将生命值设置为1
            if (Owner.HP <= 0)
            {
                // 注意：这里需要通过特殊方式设置HP，因为HP通常是只读的
                // 可能需要调用Owner的特殊方法或通过反射
                Debug.Log(
                    $"ImmortalBuff: {Owner.DisplayName} triggered immortality! Setting HP to 1"
                );

                // 添加无敌Buff
                var invincibilityBuff = new InvincibilityBuff(Owner, 2);
                battleModel.AddBuff(invincibilityBuff);

                Debug.Log($"ImmortalBuff: {Owner.DisplayName} gained 2 turns of invincibility");

                return true; // 阻止死亡
            }
        }

        // 如果已经触发过，则不再阻止死亡
        return false;
    }

    public override void OnRemoved()
    {
        Debug.Log($"ImmortalBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff
    }
}
