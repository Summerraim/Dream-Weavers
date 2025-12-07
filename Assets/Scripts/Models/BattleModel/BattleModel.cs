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

    private readonly List<Enemy> enemyUnits;
    private readonly List<SynergyModel> activeSynergies;
    private readonly Dictionary<int, int> skillCooldowns; // 技能索引 -> 剩余冷却回合数

    public BattleModel()
    {
        CurrentTurn = 0;
        enemyUnits = new List<Enemy>();
        activeSynergies = new List<SynergyModel>();
        skillCooldowns = new Dictionary<int, int>();
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
    /// 增加回合数，并更新技能冷却
    /// </summary>
    public void IncrementTurn()
    {
        CurrentTurn++;
        UpdateSkillCooldowns();
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
    }
}
