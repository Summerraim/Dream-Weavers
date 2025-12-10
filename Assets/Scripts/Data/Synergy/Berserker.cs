using UnityEngine;

/// <summary>
/// 狂战士羁绊：攻击力对比机制
/// (2) 每次释放技能时，基础攻击力低于敌方则积1层"怒意"，每层怒意+10攻击力；攻击力高于敌方则消耗所有怒意，造成10*层数的伤害
/// (4) 每层怒意变为+20攻击力
/// (6) 怒意不消耗
/// </summary>
[CreateAssetMenu(menuName = "Data/Synergy/Berserker")]
public class Berserker : Synergy
{
    [Header("2层配置")]
    [SerializeField]
    private int tierTwoDamagePerStack = 10;

    [Header("4层配置")]
    [SerializeField]
    private int tierFourDamagePerStack = 20;

    [Header("6层配置")]
    [SerializeField]
    private bool tierSixNoConsume = true;

    private static RageBuff currentRageBuff;

    /// <summary>
    /// 获取当前的怒意Buff（供BattleController调用）
    /// </summary>
    public static RageBuff CurrentRageBuff => currentRageBuff;

    public override void Apply(SynergyModel model)
    {
        if (model == null || model.Owner == null)
            return;

        int tier = model.GetCurrentTierIndex();
        if (tier < 0)
        {
            // 没有达到任何档位，移除怒意Buff
            RemoveRageBuff(model);
            return;
        }

        // 根据档位确定参数
        int damagePerStack = tierTwoDamagePerStack;
        bool consumeOnHigherDamage = true;

        switch (tier)
        {
            case 0: // 2个单位
                damagePerStack = tierTwoDamagePerStack;
                consumeOnHigherDamage = true;
                break;
            case 1: // 4个单位
                damagePerStack = tierFourDamagePerStack;
                consumeOnHigherDamage = true;
                break;
            case 2: // 6个单位
                damagePerStack = tierFourDamagePerStack; // 使用4层的攻击力
                consumeOnHigherDamage = !tierSixNoConsume; // 不消耗
                break;
        }

        // 检查是否已有怒意Buff
        var existingBuff = FindRageBuff(model.Owner);
        if (existingBuff != null)
        {
            // 更新现有Buff（需要移除并重新创建以更新参数）
            RemoveRageBuff(model);
        }

        // 创建新的怒意Buff
        var rageBuff = new RageBuff(
            model.Owner,
            GetBattleModel(),
            damagePerStack,
            consumeOnHigherDamage
        );

        // 添加到战斗系统
        var battleModel = GetBattleModel();
        if (battleModel != null)
        {
            battleModel.AddBuff(rageBuff);
            currentRageBuff = rageBuff;
        }

        Debug.Log(
            $"Berserker: Applied to {model.Owner.DisplayName}, Tier={tier}, DamagePerStack={damagePerStack}, ConsumeOnHigher={consumeOnHigherDamage}"
        );
    }

    /// <summary>
    /// 查找目标单位的怒意Buff
    /// </summary>
    private RageBuff FindRageBuff(IBattleUnit owner)
    {
        var battleModel = GetBattleModel();
        if (battleModel == null)
            return null;

        var buffs = battleModel.GetBuffsForUnit(owner);
        foreach (var buff in buffs)
        {
            if (buff is RageBuff rageBuff)
                return rageBuff;
        }
        return null;
    }

    /// <summary>
    /// 移除怒意Buff
    /// </summary>
    private void RemoveRageBuff(SynergyModel model)
    {
        var rageBuff = FindRageBuff(model.Owner);
        if (rageBuff != null)
        {
            var battleModel = GetBattleModel();
            battleModel?.RemoveBuff(rageBuff);
            if (currentRageBuff == rageBuff)
                currentRageBuff = null;
        }
    }

    /// <summary>
    /// 获取当前战斗模型（通过静态引用或其他方式）
    /// 这里假设可以通过某种方式获取，实际可能需要调整
    /// </summary>
    private BattleModel GetBattleModel()
    {
        // 这里需要一个方式来获取当前的BattleModel
        // 可以考虑添加一个静态引用，类似于其他Effect的CurrentBattle
        // 暂时返回null，需要在BattleController中设置
        return BerserkerSynergyBridge.CurrentBattle;
    }
}

/// <summary>
/// 用于桥接Berserker Synergy和BattleModel的静态类
/// </summary>
public static class BerserkerSynergyBridge
{
    public static BattleModel CurrentBattle { get; set; }
}
