using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Targeted Attack Boost (200%)")]
/// <summary>
/// 仅对指定的 Spirit 生效的攻击力提升道具：固定增加 200% 基础攻击力。
/// </summary>
public class TargetedAttackBoost : Effect
{
    [Header("仅指定的 Spirit 会获得加成")]
    public SpiritData targetSpirit;

    [SerializeField, Range(0f, 5f)]
    private float attackBonusMultiplier = 2f; // 200%

    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        var battle = CurrentBattle ?? BattleModel.ActiveBattle;
        if (battle == null)
        {
            Debug.LogWarning("TargetedAttackBoost: No active battle model found");
            return;
        }

        if (targetSpirit == null)
        {
            Debug.LogWarning("TargetedAttackBoost: No target SpiritData assigned");
            return;
        }

        // 如果玩家数据中没有该精灵，则不生效
        var ownedSpirits = PlayerManager.Instance?.GetOwnedSpirits();
        if (ownedSpirits == null || ownedSpirits.Count == 0)
        {
            Debug.LogWarning("TargetedAttackBoost: Player has no spirits (PlayerData empty)");
            return;
        }

        if (!ownedSpirits.Contains(targetSpirit))
        {
            Debug.Log("TargetedAttackBoost: Configured spirit not owned by player, skipping effect");
            return;
        }

        // 优先使用指向的目标，其次回退到施法者，确保不因空目标而丢失效果
        var receiverSpirit = target as Spirit ?? caster as Spirit;
        if (receiverSpirit == null)
        {
            Debug.LogWarning("TargetedAttackBoost: No Spirit target to buff");
            return;
        }

        if (receiverSpirit.Data != targetSpirit)
        {
            Debug.Log(
                $"TargetedAttackBoost: Spirit {receiverSpirit.DisplayName} does not match configured target {targetSpirit.DisplayName}"
            );
            return;
        }

        var buff = new AttackBuff(receiverSpirit, attackBonusMultiplier);
        battle.AddBuff(buff);
    }
}
