using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Invincibility")]
// 无敌：一定回合内不受任何伤害
public class Invincibility : Effect
{
    [SerializeField, Min(1)]
    private int duration = 2;

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
            Debug.LogWarning("ApplyInvincibilityBuff: No active battle model found");
            return;
        }

        var buff = new InvincibilityBuff(receiver, duration);
        CurrentBattle.AddBuff(buff);
    }
}
