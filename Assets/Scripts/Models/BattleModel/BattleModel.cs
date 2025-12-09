using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录单局战斗的临时数据
/// </summary>
public class BattleModel
{
    /// <summary>
    /// 当前回合数
    /// </summary>
    public int CurrentTurn { get; private set; }

    /// <summary>
    /// 场上玩家单位
    /// </summary>
    public Spirit PlayerUnit { get; private set; }

    /// <summary>
    /// 场上敌方单位列表
    /// </summary>
    public IReadOnlyList<Enemy> EnemyUnits => enemyUnits;

    /// <summary>
    /// 激活的羁绊列表
    /// </summary>
    public IReadOnlyList<SynergyModel> ActiveSynergies => activeSynergies;

    /// <summary>
    /// 所有活跃的Buff列表
    /// </summary>
    public IReadOnlyList<Buff> ActiveBuffs => activeBuffs;

    private readonly List<Enemy> enemyUnits;
    private readonly List<SynergyModel> activeSynergies;
    private readonly Dictionary<int, int> skillCooldowns; // 技能索引 -> 剩余冷却回合数
    private readonly Dictionary<int, int> skillUsageCount; // 技能索引 -> 当前战斗已使用次数
    private readonly List<Buff> activeBuffs; // 所有活跃的Buff

    public BattleModel()
    {
        CurrentTurn = 0;
        enemyUnits = new List<Enemy>();
        activeSynergies = new List<SynergyModel>();
        skillCooldowns = new Dictionary<int, int>();
        skillUsageCount = new Dictionary<int, int>();
        activeBuffs = new List<Buff>();
    }

    /// <summary>
    /// 初始化战斗记录
    /// </summary>
    public void InitializeBattle(Spirit player, Enemy enemy)
    {
        PlayerUnit = player;
        enemyUnits.Clear();
        if (enemy != null)
        {
            enemyUnits.Add(enemy);
        }
        CurrentTurn = 1;

        UpdateActiveSynergies();
    }

    /// <summary>
    /// 更新玩家单位（用于Spirit切换）
    /// </summary>
    public void UpdatePlayer(Spirit newPlayer)
    {
        // 先保存旧的玩家单位引用
        var oldPlayer = PlayerUnit;

        // 更新玩家单位
        PlayerUnit = newPlayer;

        // 清除旧Spirit的技能冷却和使用次数
        skillCooldowns.Clear();
        skillUsageCount.Clear();

        // 移除旧Spirit的所有Buff
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].Owner == oldPlayer)
            {
                var buff = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                buff.OnRemoved();
            }
        }

        UpdateActiveSynergies();
        Debug.Log($"BattleModel: Player updated to {newPlayer?.DisplayName}");
    }

    /// <summary>
    /// 增加回合数，并更新技能冷却和Buff
    /// </summary>
    public void IncrementTurn()
    {
        CurrentTurn++;

        // 回合开始时触发Buff效果
        TriggerBuffsOnTurnStart();

        // 更新技能冷却
        UpdateSkillCooldowns();
    }

    /// <summary>
    /// 回合结束时调用，处理Buff持续时间
    /// </summary>
    public void OnTurnEnd()
    {
        TriggerBuffsOnTurnEnd();
        RemoveExpiredBuffs();
    }

    /// <summary>
    /// 更新所有技能的冷却时间（每回合减1）
    /// </summary>
    private void UpdateSkillCooldowns()
    {
        var keys = new List<int>(skillCooldowns.Keys);
        foreach (var skillIndex in keys)
        {
            skillCooldowns[skillIndex] = Mathf.Max(0, skillCooldowns[skillIndex] - 1);
            if (skillCooldowns[skillIndex] == 0)
            {
                skillCooldowns.Remove(skillIndex);
            }
        }
    }

    /// <summary>
    /// 设置技能冷却
    /// </summary>
    public void SetSkillCooldown(int skillIndex, int cooldownTurns)
    {
        if (cooldownTurns > 0)
        {
            skillCooldowns[skillIndex] = cooldownTurns;
        }
        else
        {
            skillCooldowns.Remove(skillIndex);
        }
    }

    /// <summary>
    /// 获取技能剩余冷却回合数
    /// </summary>
    public int GetSkillCooldown(int skillIndex)
    {
        return skillCooldowns.TryGetValue(skillIndex, out var cooldown) ? cooldown : 0;
    }

    /// <summary>
    /// 检查技能是否在冷却中
    /// </summary>
    public bool IsSkillOnCooldown(int skillIndex)
    {
        return GetSkillCooldown(skillIndex) > 0;
    }

    /// <summary>
    /// 记录技能使用次数
    /// </summary>
    public void IncrementSkillUsage(int skillIndex)
    {
        if (!skillUsageCount.ContainsKey(skillIndex))
        {
            skillUsageCount[skillIndex] = 0;
        }
        skillUsageCount[skillIndex]++;
    }

    /// <summary>
    /// 获取技能当前战斗已使用次数
    /// </summary>
    public int GetSkillUsageCount(int skillIndex)
    {
        return skillUsageCount.TryGetValue(skillIndex, out var count) ? count : 0;
    }

    /// <summary>
    /// 获取技能剩余可用次数（-1表示无限制）
    /// </summary>
    public int GetSkillRemainingUses(int skillIndex, ISkill skill)
    {
        if (skill == null || skill.MaxUsesPerBattle == 0)
            return -1; // 无限制

        int used = GetSkillUsageCount(skillIndex);
        return Mathf.Max(0, skill.MaxUsesPerBattle - used);
    }

    /// <summary>
    /// 检查技能是否已达使用次数上限
    /// </summary>
    public bool IsSkillUsageLimitReached(int skillIndex, ISkill skill)
    {
        if (skill == null || skill.MaxUsesPerBattle == 0)
            return false; // 无限制

        return GetSkillUsageCount(skillIndex) >= skill.MaxUsesPerBattle;
    }

    /// <summary>
    /// 更新激活的羁绊列表
    /// </summary>
    public void UpdateActiveSynergies()
    {
        activeSynergies.Clear();

        if (PlayerUnit?.Synergies != null)
        {
            foreach (var synergyModel in PlayerUnit.Synergies)
            {
                if (synergyModel.GetCurrentTierIndex() >= 0)
                {
                    activeSynergies.Add(synergyModel);
                }
            }
        }
    }

    /// <summary>
    /// 添加敌方单位
    /// </summary>
    public void AddEnemy(Enemy enemy)
    {
        if (enemy != null && !enemyUnits.Contains(enemy))
        {
            enemyUnits.Add(enemy);
        }
    }

    /// <summary>
    /// 移除敌方单位
    /// </summary>
    public void RemoveEnemy(Enemy enemy)
    {
        enemyUnits.Remove(enemy);
    }

    /// <summary>
    /// 获取场上所有存活的敌方单位数量
    /// </summary>
    public int GetAliveEnemyCount()
    {
        int count = 0;
        foreach (var enemy in enemyUnits)
        {
            if (enemy != null && !enemy.IsDead)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 重置战斗记录
    /// </summary>
    public void Reset()
    {
        CurrentTurn = 0;
        PlayerUnit = null;
        enemyUnits.Clear();
        activeSynergies.Clear();
        skillCooldowns.Clear();
        skillUsageCount.Clear();
        activeBuffs.Clear();
    }

    /// <summary>
    /// 添加Buff到战斗中
    /// </summary>
    public void AddBuff(Buff buff)
    {
        if (buff == null)
            return;

        activeBuffs.Add(buff);
        buff.OnApplied();
        Debug.Log(
            $"Buff {buff.DisplayName} applied to {buff.Owner?.DisplayName}, duration: {buff.RemainingTurns} turns"
        );
    }

    /// <summary>
    /// 移除指定的Buff
    /// </summary>
    public void RemoveBuff(Buff buff)
    {
        if (buff == null)
            return;

        if (activeBuffs.Remove(buff))
        {
            buff.OnRemoved();
            Debug.Log($"Buff {buff.DisplayName} removed from {buff.Owner?.DisplayName}");
        }
    }

    /// <summary>
    /// 获取指定单位的所有Buff
    /// </summary>
    public List<Buff> GetBuffsForUnit(IBattleUnit unit)
    {
        List<Buff> result = new List<Buff>();
        if (unit == null)
            return result;

        foreach (var buff in activeBuffs)
        {
            if (buff.Owner == unit)
            {
                result.Add(buff);
            }
        }
        return result;
    }

    /// <summary>
    /// 触发所有Buff的回合开始效果
    /// </summary>
    private void TriggerBuffsOnTurnStart()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (i < activeBuffs.Count)
            {
                activeBuffs[i].OnTurnStart();
            }
        }
    }

    /// <summary>
    /// 触发所有Buff的回合结束效果
    /// </summary>
    private void TriggerBuffsOnTurnEnd()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (i < activeBuffs.Count)
            {
                activeBuffs[i].OnTurnEnd();
            }
        }
    }

    /// <summary>
    /// 移除已过期的Buff
    /// </summary>
    private void RemoveExpiredBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].IsExpired)
            {
                var buff = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                buff.OnRemoved();
                Debug.Log(
                    $"Buff {buff.DisplayName} expired and removed from {buff.Owner?.DisplayName}"
                );
            }
        }
    }

    /// <summary>
    /// 计算单位的总攻击力加成（来自Buff）
    /// </summary>
    public int GetTotalDamageBonus(IBattleUnit unit)
    {
        int total = 0;
        foreach (var buff in GetBuffsForUnit(unit))
        {
            total += buff.GetDamageBonus();
        }
        return total;
    }

    /// <summary>
    /// 计算单位的总防御力加成（来自Buff）
    /// </summary>
    public int GetTotalDefenseBonus(IBattleUnit unit)
    {
        int total = 0;
        foreach (var buff in GetBuffsForUnit(unit))
        {
            total += buff.GetDefenseBonus();
        }
        return total;
    }

    /// <summary>
    /// 检查单位是否被控制（无法行动）
    /// </summary>
    public bool IsUnitControlled(IBattleUnit unit)
    {
        if (unit == null)
            return false;

        foreach (var buff in GetBuffsForUnit(unit))
        {
            // 检查是否有控制型Debuff
            if (buff is FrozenDebuff || buff is SleepDebuff)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取单位的控制效果名称（用于显示）
    /// </summary>
    public string GetControlEffectName(IBattleUnit unit)
    {
        if (unit == null)
            return string.Empty;

        foreach (var buff in GetBuffsForUnit(unit))
        {
            if (buff is FrozenDebuff || buff is SleepDebuff )
            {
                return buff.DisplayName;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 检查单位是否混乱（可能攻击自己）
    /// </summary>
    public bool CheckConfusion(IBattleUnit unit)
    {
        if (unit == null)
            return false;

        foreach (var buff in GetBuffsForUnit(unit))
        {
            if (buff is ConfusionDebuff confusion)
            {
                return confusion.CheckConfusion();
            }
        }
        return false;
    }
}
