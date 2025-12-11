using UnityEngine;

/// <summary>
/// 精灵进化/替换 Buff：一次性触发，将拥有者替换为指定的 SpiritData 对应的 Spirit。
/// </summary>
public class EvolutionBuff : Buff
{
    public override string DisplayName => "进化";
    public override string Description => "将精灵替换为另一形态";

    private readonly BattleModel battleModel;
    private readonly SpiritData targetSpiritData;

    /// <param name="owner">Buff 拥有者（通常是当前玩家单位）</param>
    /// <param name="duration">持续时间，进化为一次性效果，传入值将被视为一次性触发</param>
    /// <param name="battleModel">当前战斗模型，用于执行单位替换</param>
    /// <param name="targetSpiritData">要替换成的 SpiritData</param>
    public EvolutionBuff(IBattleUnit owner, int duration, BattleModel battleModel, SpiritData targetSpiritData, Effect sourceEffect = null)
        : base(owner, duration, sourceEffect)
    {
        this.battleModel = battleModel;
        this.targetSpiritData = targetSpiritData;
        IsOneTime = true;
    }

    public override void OnApplied()
    {
        // 安全检查
        if (battleModel == null)
        {
            Debug.LogWarning("EvolutionBuff: battleModel is null");
            return;
        }
        if (targetSpiritData == null)
        {
            Debug.LogWarning("EvolutionBuff: targetSpiritData is null");
            return;
        }

        // 仅当玩家单位与拥有者一致时执行替换（当前系统仅支持替换玩家单位）
        if (battleModel.PlayerUnit != null && Owner == battleModel.PlayerUnit)
        {
            var newSpirit = new Spirit(targetSpiritData);
            battleModel.UpdatePlayer(newSpirit);
            Debug.Log($"EvolutionBuff: {Owner?.DisplayName} 进化为 {newSpirit.DisplayName}");
        }
        else
        {
            Debug.LogWarning("EvolutionBuff: Owner is not current player unit; skip evolution");
        }

        HasTriggered = true;
    }

    public override void OnTurnEnd()
    {
        // 一次性 Buff：触发后立即过期
        base.OnTurnEnd();
    }
}
