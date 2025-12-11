using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Defense Up (10%)")]
// 防御提升：提升防御力10%，可作用于场上所有单位
public class DefenseUp : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float defensePercent = 0.10f;

    [SerializeField, Min(1)]
    private int duration = 3; // 默认持续3回合，如需永久可设更大或-1

    [SerializeField]
    private bool applyToAllUnitsOnField = true; // 仅对我方（玩家单位）或单体生效

    [SerializeField]
    private bool applyToCasterIfNotAll = true; // 非全体时应用给施法者或目标

    // 与其它Effect保持一致的临时方案：由战斗系统在开局/切换时设置
    public static BattleModel CurrentBattle { get; set; }

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        if (CurrentBattle == null)
        {
            Debug.LogWarning("DefenseUp: No active battle model found");
            return;
        }

        if (applyToAllUnitsOnField)
        {
            // 仅对我方单位（玩家精灵）施加效果
            if (CurrentBattle.PlayerUnit != null)
            {
                var playerBuff = new DefenseUpBuff(CurrentBattle.PlayerUnit, duration, defensePercent);
                CurrentBattle.AddBuff(playerBuff);
            }
        }
        else
        {
            IBattleUnit receiver = applyToCasterIfNotAll ? caster : target;
            if (receiver == null)
                return;

            var buff = new DefenseUpBuff(receiver, duration, defensePercent);
            CurrentBattle.AddBuff(buff);
        }
    }
}
