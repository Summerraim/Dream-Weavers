using UnityEngine;

/// <summary>
/// 狩猎大师Buff：诱捕，可在敌方生命值/法力值 ≤ 10% 时捕捉成功
/// </summary>
public class HunterMasterBuff : Buff
{
    public override string DisplayName => "诱捕";
    public override string Description =>
        $"可在敌方生命值或法力值 ≤ {captureThreshold * 100}% 时捕捉成功";

    private float captureThreshold;
    private BattleModel battleModel;
    private Synergy hunterMasterSynergy;

    public HunterMasterBuff(
        IBattleUnit owner,
        float captureThreshold,
        BattleModel battleModel,
        Synergy hunterMasterSynergy
    )
        : base(owner, -1) // 永久Buff
    {
        this.captureThreshold = captureThreshold;
        this.battleModel = battleModel;
        this.hunterMasterSynergy = hunterMasterSynergy;
    }

    public override void OnApplied()
    {
        Debug.Log(
            $"HunterMasterBuff: Applied to {Owner?.DisplayName}, capture threshold: {captureThreshold * 100}%"
        );
    }

    /// <summary>
    /// 检查敌人是否满足诱捕条件（HP或Mana ≤ 阈值）
    /// </summary>
    public bool CanCaptureEnemy(IBattleUnit enemy)
    {
        if (enemy == null)
            return false;

        float hpPercent = (float)enemy.HP / enemy.MaxHP;
        float manaPercent = (float)enemy.Mana / enemy.MaxMana;

        bool canCapture = hpPercent <= captureThreshold || manaPercent <= captureThreshold;

        if (canCapture)
        {
            Debug.Log(
                $"HunterMasterBuff: Enemy {enemy.DisplayName} can be captured! HP: {hpPercent * 100:F1}%, Mana: {manaPercent * 100:F1}%"
            );
        }

        return canCapture;
    }

    public override void OnRemoved()
    {
        Debug.Log($"HunterMasterBuff: Removed from {Owner?.DisplayName}");
    }

    public override void OnTurnEnd()
    {
        // 永久Buff，无需每回合处理
    }
}
