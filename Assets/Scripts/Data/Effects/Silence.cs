using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Debuff/Silence")]
// 沉默：增加技能法力消耗
public class Silence : Effect
{
    [SerializeField, Range(0f, 2f)]
    private float manaIncrease = 0.5f; // 法力消耗增加50%

    [SerializeField, Min(1)]
    private int duration = 2;

    [SerializeField]
    private bool applyToTarget = true;

    public override void Apply(IBattleUnit caster, IBattleUnit target)
    {
        IBattleUnit receiver = applyToTarget ? target : caster;
        if (receiver == null)
            return;

        if (BattleModel.ActiveBattle == null)
        {
            Debug.LogWarning("Silence: No active battle model found");
            return;
        }

        var debuff = new SilenceDebuff(receiver, duration, manaIncrease, this);
        BattleModel.ActiveBattle.AddBuff(debuff);
    }
}
