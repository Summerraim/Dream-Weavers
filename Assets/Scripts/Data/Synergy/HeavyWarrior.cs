using UnityEngine;

/// <summary>
/// 重装战士羁绊：提升最大生命值
/// (2) +10% 最大生命值
/// (4) +20% 最大生命值
/// (6) +40% 最大生命值
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/HeavyWarrior")]
public class HeavyWarrior : Synergy
{
    [Header("2层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierTwoBonus = 0.1f;

    [Header("4层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierFourBonus = 0.2f;

    [Header("6层配置")]
    [SerializeField, Range(0f, 1f)]
    private float tierSixBonus = 0.4f;

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        // SetMaxHpBonusPercent是Spirit类的方法，需要转换类型
        var spirit = model.Owner as Spirit;
        if (spirit == null)
        {
            Debug.LogWarning($"HeavyWarrior: Owner {model.Owner.DisplayName} is not a Spirit, cannot apply bonus");
            return;
        }

        float bonus = 0f;
        int tier = model.GetCurrentTierIndex();
        switch (tier)
        {
            case 0: // 2个单位
                bonus = tierTwoBonus;
                break;
            case 1: // 4个单位
                bonus = tierFourBonus;
                break;
            case 2: // 6个单位
                bonus = tierSixBonus;
                break;
            default:
                bonus = 0f;
                break;
        }

        spirit.SetMaxHpBonusPercent(bonus);
        Debug.Log(
            $"HeavyWarrior: Applied to {model.Owner.DisplayName}, Tier={tier}, Bonus={bonus * 100}%, MaxHP={model.Owner.MaxHP}"
        );
    }
}
