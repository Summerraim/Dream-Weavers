using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Vampiric")]
// 吸血：攻击造成的伤害一定比例转化为自身生命值
public class Vampiric : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float lifeStealPercent = 0.3f;

    [SerializeField, Min(1)]
    private int duration = 3;

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
            Debug.LogWarning("ApplyVampiricBuff: No active battle model found");
            return;
        }

        var buff = new VampiricBuff(receiver, duration, lifeStealPercent);
        CurrentBattle.AddBuff(buff);
    }
}
