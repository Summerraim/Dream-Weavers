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

    public BattleModel()
    {
        CurrentTurn = 0;
        enemyUnits = new List<Enemy>();
        activeSynergies = new List<SynergyModel>();
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
    /// 增加回合数
    /// </summary>
    public void IncrementTurn()
    {
        CurrentTurn++;
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
    }
}
