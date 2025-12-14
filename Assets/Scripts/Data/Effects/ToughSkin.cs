using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Tough Skin")]
// 坚韧皮肤：减伤20%
public class ToughSkin : Effect
{
    [SerializeField, Range(0f, 1f)]
    private float damageReduction = 0.2f;

    [SerializeField, Min(1)]
    private int duration = 999; // 默认持久

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
            Debug.LogWarning("ApplyToughSkinBuff: No active battle model found");
            return;
        }

        var buff = new ToughSkinBuff(receiver, duration, damageReduction, this);
        CurrentBattle.AddBuff(buff);
    }
}
