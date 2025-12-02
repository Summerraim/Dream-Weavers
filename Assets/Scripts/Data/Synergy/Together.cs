using UnityEngine;

[CreateAssetMenu(menuName = "Data/Synergy/Together")]
public class Together : Synergy
{
    [SerializeField, Range(0f, 1f)]
    private float tierTwoBonus = 0.1f;

    [SerializeField, Range(0f, 1f)]
    private float tierFourBonus = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float tierSixBonus = 0.1f;

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        float bonus = 0f;
        int tier = model.GetCurrentTierIndex();
        switch (tier)
        {
            case 0:
                bonus = tierTwoBonus;
                break;
            case 1:
                bonus = tierFourBonus;
                break;
            case 2:
                bonus = tierSixBonus;
                break;
            default:
                bonus = 0f;
                break;
        }

        model.Owner.SetMaxHpBonusPercent(bonus);
    }
}
