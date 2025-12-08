using UnityEngine;

[CreateAssetMenu(menuName = "Data/Effects/Buff/Thorns")]
// 反伤：受到攻击时，对攻击者造成固定或比例的反伤
public class Thorns : Effect
{
    [SerializeField]
    private bool usePercentDamage = true;

    [SerializeField, Range(0f, 1f)]
    private float reflectPercent = 0.2f;

    [SerializeField, Min(0)]
    private int flatDamage = 50;

    [SerializeField, Min(1)]
    private int duration = 5;

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
            Debug.LogWarning("ApplyThornsBuff: No active battle model found");
            return;
        }

        Buff buff;
        if (usePercentDamage)
        {
            buff = new ThornsBuff(receiver, duration, reflectPercent);
        }
        else
        {
            buff = new ThornsBuff(receiver, duration, flatDamage);
        }

        CurrentBattle.AddBuff(buff);
    }
}
