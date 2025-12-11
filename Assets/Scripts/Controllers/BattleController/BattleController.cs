using UnityEngine;

public enum BattleState
{
    None,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat,
}

public class BattleController : MonoBehaviour
{
    [SerializeField]
    private PlayerData playerData;

    [SerializeField]
    private EnemyData enemyData;

    private Spirit player;
    private Enemy enemy;
    private AIController enemyAI;

    [SerializeField]
    private UI_BattleView battleView;

    [SerializeField]
    private UI_SpiritSwitcher spiritSwitcher;

    [SerializeField]
    private UI_EffectDisplay effectDisplay;

    private BattleModel model;

    // Spirit队列系统
    private System.Collections.Generic.List<SpiritData> spiritQueue;
    private int currentSpiritIndex = 0;
    private System.Collections.Generic.Dictionary<int, bool> spiritAliveStatus; // 跟踪每个Spirit的存活状态
    private System.Collections.Generic.Dictionary<int, SpiritRuntimeData> spiritRuntimeData; // 跟踪每个Spirit的运行时数据（HP/MP）

    // 缓存的 HP/Mana 值，用于检测变化
    private int lastPlayerHP;
    private int lastPlayerMana;
    private int lastEnemyHP;
    private int lastEnemyMana;

    public BattleState State { get; private set; } = BattleState.None;

    private void Start()
    {
        // 场景加载时自动初始化战斗
        InitializeBattle();
        Debug.Log("BattleController: InitializeBattle called in Start()");
    }

    public void InitializeBattle()
    {
        // 从PlayerData获取出场的Spirit队列
        if (playerData == null)
        {
            Debug.LogError("BattleController: PlayerData is null!");
            return;
        }

        spiritQueue = playerData.GetDeployedSpirits();
        if (spiritQueue == null || spiritQueue.Count == 0)
        {
            Debug.LogError("BattleController: No spirits deployed in PlayerData!");
            return;
        }

        currentSpiritIndex = 0;

        // 初始化Spirit存活状态和运行时数据
        spiritAliveStatus = new System.Collections.Generic.Dictionary<int, bool>();
        spiritRuntimeData = new System.Collections.Generic.Dictionary<int, SpiritRuntimeData>();

        for (int i = 0; i < spiritQueue.Count; i++)
        {
            spiritAliveStatus[i] = true; // 战斗开始时所有Spirit都是存活的

            // 初始化运行时数据（使用SpiritData的基础值）
            var data = spiritQueue[i];
            spiritRuntimeData[i] = new SpiritRuntimeData
            {
                CurrentHP = data.MaxHP,
                MaxHP = data.MaxHP,
                CurrentMP = data.MaxMana,
                MaxMP = data.MaxMana,
            };
        }

        // 创建第一个Spirit
        player = new Spirit(spiritQueue[currentSpiritIndex]);
        Debug.Log(
            $"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName}"
        );

        enemy = new Enemy(enemyData);
        enemyAI = new AIController();

        // 创建并初始化战斗模型，由本 Controller 管理
        model = new BattleModel();
        model.InitializeBattle(player, enemy);

        // 设置Buff系统的静态引用
        Strengthen.CurrentBattle = model;
        ToughSkin.CurrentBattle = model;
        ManaRegeneration.CurrentBattle = model;
        HealthRegeneration.CurrentBattle = model;
        Vampiric.CurrentBattle = model;
        Thorns.CurrentBattle = model;
        Revive.CurrentBattle = model;
        Invincibility.CurrentBattle = model;
        ManaShield.CurrentBattle = model;
        Shield.CurrentBattle = model;
        CriticalStrike.CurrentBattle = model;

        // 设置Debuff系统的静态引用
        WeakenAttack.CurrentBattle = model;
        WeakenDefense.CurrentBattle = model;
        Weaken.CurrentBattle = model;
        ManaLeech.CurrentBattle = model;
        HealingReduction.CurrentBattle = model;
        Vulnerability.CurrentBattle = model;
        Poison.CurrentBattle = model;
        Burn.CurrentBattle = model;
        Blind.CurrentBattle = model;
        Silence.CurrentBattle = model;
        Curse.CurrentBattle = model;

        // 设置ControlDebuff系统的静态引用
        Frozen.CurrentBattle = model;
        Sleep.CurrentBattle = model;
        Confusion.CurrentBattle = model;

        // 设置Special系统的静态引用
        PrepareEffect.CurrentBattle = model;

        // 设置净化/驱散系统的静态引用
        Cleanse.CurrentBattle = model;
        Dispel.CurrentBattle = model;

        // 设置Berserker Synergy的静态引用
        BerserkerSynergyBridge.CurrentBattle = model;

        // 设置Sacrifice Synergy的静态引用（用于获取所有出场Spirit）
        SacrificeSynergyBridge.DeployedSpirits = spiritQueue;

        // 初始化全队羁绊系统（战斗开始时统计所有出场Spirit的Synergy并应用效果）
        model.InitializeTeamSynergies(spiritQueue);
        Debug.Log("BattleController: Team synergies initialized");

        // 绑定 UI（如果存在）
        if (battleView != null)
            battleView.Bind(this, model);

        // 绑定Spirit切换器（如果存在）
        if (spiritSwitcher != null)
            spiritSwitcher.Bind(this);

        // 绑定Effect显示器（如果存在）
        if (effectDisplay != null)
        {
            effectDisplay.Bind(this, model);
            Debug.Log("BattleController: UI_EffectDisplay已绑定");
        }
        else
        {
            Debug.LogWarning("BattleController: effectDisplay is null! 请在Inspector中拖入UI_EffectDisplay组件");
        }

        // 初始化缓存值
        lastPlayerHP = player?.HP ?? 0;
        lastPlayerMana = player?.Mana ?? 0;
        lastEnemyHP = enemy?.HP ?? 0;
        lastEnemyMana = enemy?.Mana ?? 0;

        State = BattleState.PlayerTurn;
    }

    /// <summary>
    /// Allows external systems (ex: rooms) to begin a battle with runtime data.
    /// </summary>
    public void BeginBattleWith(PlayerData playerDataOverride, EnemyData enemyDataOverride)
    {
        if (playerDataOverride == null)
        {
            Debug.LogWarning("BattleController: BeginBattleWith called without PlayerData");
            return;
        }

        if (enemyDataOverride == null)
        {
            Debug.LogWarning("BattleController: BeginBattleWith called without EnemyData");
            return;
        }

        playerData = playerDataOverride;
        enemyData = enemyDataOverride;

        InitializeBattle();
    }

    public Spirit Player => player;
    public Enemy Enemy => enemy;

    /// <summary>
    /// 获取部署的Spirit列表
    /// </summary>
    public System.Collections.Generic.List<SpiritData> GetDeployedSpirits()
    {
        return spiritQueue;
    }

    /// <summary>
    /// 获取当前Spirit的索引
    /// </summary>
    public int GetCurrentSpiritIndex()
    {
        return currentSpiritIndex;
    }

    /// <summary>
    /// 检查指定索引的Spirit是否存活
    /// </summary>
    public bool IsSpiritAlive(int index)
    {
        if (spiritAliveStatus == null || !spiritAliveStatus.ContainsKey(index))
            return false;
        return spiritAliveStatus[index];
    }

    /// <summary>
    /// 获取指定索引Spirit的运行时数据（HP/MP）
    /// </summary>
    public SpiritRuntimeData GetSpiritRuntimeData(int index)
    {
        // 如果是当前Spirit，返回实时数据
        if (index == currentSpiritIndex && player != null)
        {
            return new SpiritRuntimeData
            {
                CurrentHP = player.HP,
                MaxHP = player.MaxHP,
                CurrentMP = player.Mana,
                MaxMP = player.MaxMana,
            };
        }

        // 否则返回缓存的数据
        if (spiritRuntimeData != null && spiritRuntimeData.ContainsKey(index))
        {
            return spiritRuntimeData[index];
        }

        // 如果没有数据，返回默认值
        return new SpiritRuntimeData();
    }

    /// <summary>
    /// 获取当前Spirit在队列中的索引（从1开始）
    /// </summary>
    public int GetCurrentSpiritNumber()
    {
        return currentSpiritIndex + 1;
    }

    /// <summary>
    /// 获取总Spirit数量
    /// </summary>
    public int GetTotalSpiritCount()
    {
        return spiritQueue?.Count ?? 0;
    }

    /// <summary>
    /// 获取剩余Spirit数量（包括当前的）
    /// </summary>
    public int GetRemainingSpiritCount()
    {
        if (spiritQueue == null)
            return 0;
        return spiritQueue.Count - currentSpiritIndex;
    }

    public void PlayerUseSkill(ISkill skill)
    {
        PlayerUseSkill(skill, -1); // -1 表示不追踪冷却（旧版本兼容）
    }

    private void PlayerUseSkill(ISkill skill, int skillIndex)
    {
        if (State != BattleState.PlayerTurn || skill == null || player == null || enemy == null)
        {
            Debug.Log(
                $"BattleController: PlayerUseSkill failed - State check failed or null units"
            );
            return;
        }

        // 检查是否被控制（冰冻、睡眠等）
        if (model != null && model.IsUnitControlled(player))
        {
            string controlEffect = model.GetControlEffectName(player);
            Debug.Log(
                $"BattleController: {player.DisplayName} 被 {controlEffect} 控制，无法行动！"
            );
            return;
        }

        // 检查冷却（如果提供了技能索引）
        if (skillIndex >= 0 && model != null && model.IsSkillOnCooldown(skillIndex))
        {
            Debug.Log(
                $"BattleController: Skill {skillIndex} is on cooldown for {model.GetSkillCooldown(skillIndex)} more turns"
            );
            return;
        }

        // 检查使用次数限制（如果提供了技能索引）
        if (skillIndex >= 0 && model != null && model.IsSkillUsageLimitReached(skillIndex, skill))
        {
            int maxUses = skill.MaxUsesPerBattle;
            Debug.Log(
                $"BattleController: Skill {skillIndex} has reached max uses per battle ({maxUses})"
            );
            return;
        }

        if (player.Mana < skill.ManaCost)
        {
            Debug.Log(
                $"BattleController: Not enough mana. Required: {skill.ManaCost}, Current: {player.Mana}"
            );
            return;
        }

        Debug.Log(
            $"BattleController: PlayerUseSkill called. Skill={skill.DisplayName}, ManaCost={skill.ManaCost}"
        );

        // 扣除蓝量
        player.ConsumeMana(skill.ManaCost);
        Debug.Log($"BattleController: Mana consumed. Remaining: {player.Mana}");

        // 执行技能
        Debug.Log($"BattleController: Executing skill on enemy...");
        skill.Execute(player, enemy);
        Debug.Log($"BattleController: Enemy HP after skill: {enemy.HP}");

        // 触发狂战士羁绊的怒意机制（如果存在）
        TriggerBerserkerRage();

        // 触发疗愈者羁绊效果（如果存在）
        TriggerHealerSynergy();

        // 记录使用次数（如果提供了技能索引）
        if (skillIndex >= 0 && model != null)
        {
            model.IncrementSkillUsage(skillIndex);
        }

        // 设置冷却（如果提供了技能索引）
        if (skillIndex >= 0 && model != null && skill.CooldownTurns > 0)
        {
            model.SetSkillCooldown(skillIndex, skill.CooldownTurns);
            Debug.Log(
                $"BattleController: Skill {skillIndex} set on cooldown for {skill.CooldownTurns} turns"
            );
        }

        // 更新模型中的羁绊/状态并刷新 UI
        model?.UpdateActiveSynergies();
        UpdateBattleStateAfterAction();
        if (battleView != null)
            battleView.Refresh();
        if (effectDisplay != null)
            effectDisplay.RefreshDisplay();
    }

    /// <summary>
    /// 尝试使用玩家的指定索引的技能（由 UI 调用）。
    /// </summary>
    /// <param name="skillIndex">技能索引（0-2）</param>
    public void UsePlayerSkill(int skillIndex)
    {
        Debug.Log($"BattleController: UsePlayerSkill called with index {skillIndex}");

        if (player == null)
        {
            Debug.LogWarning("BattleController: Player is null!");
            return;
        }

        var skills = player.GetSkills();
        Debug.Log($"BattleController: Found {skills?.Count ?? 0} skills");

        if (skills == null || skills.Count == 0)
        {
            Debug.LogWarning("BattleController: No skills available!");
            return;
        }

        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            Debug.LogWarning(
                $"BattleController: Skill index {skillIndex} out of range (0-{skills.Count - 1})"
            );
            return;
        }

        var skill = skills[skillIndex];
        if (skill == null)
        {
            Debug.LogWarning($"BattleController: Skill at index {skillIndex} is null!");
            return;
        }

        Debug.Log($"BattleController: Using skill {skillIndex}: {skill.DisplayName}");
        PlayerUseSkill(skill, skillIndex);
    }

    /// <summary>
    /// 尝试使用玩家的第一个技能（由 UI 调用）。保留用于向后兼容。
    /// </summary>
    [System.Obsolete("Use UsePlayerSkill(int skillIndex) instead")]
    public void UseFirstPlayerSkill()
    {
        UsePlayerSkill(0);
    }

    public void EndPlayerTurn()
    {
        if (State != BattleState.PlayerTurn)
            return;

        Debug.Log("BattleController: EndPlayerTurn called.");

        // 玩家回合结束，处理Buff效果
        model?.OnTurnEnd();

        // 增加回合计数（模型负责）
        model?.IncrementTurn();

        State = BattleState.EnemyTurn;
        EnemyAct();

        if (battleView != null)
            battleView.Refresh();
        if (effectDisplay != null)
            effectDisplay.RefreshDisplay();
    }

    private void EnemyAct()
    {
        if (enemyAI == null || enemy == null || player == null)
            return;

        enemyAI.TakeTurn(enemy, player);
        model?.UpdateActiveSynergies();
        UpdateBattleStateAfterAction();

        if (State == BattleState.EnemyTurn)
            State = BattleState.PlayerTurn;

        if (battleView != null)
            battleView.Refresh();
        if (effectDisplay != null)
            effectDisplay.RefreshDisplay();
    }

    private void UpdateBattleStateAfterAction()
    {
        if (enemy != null && enemy.IsDead)
        {
            State = BattleState.Victory;
            return;
        }

        if (player != null && player.IsDead)
        {
            // 标记当前Spirit为死亡
            if (spiritAliveStatus != null && spiritAliveStatus.ContainsKey(currentSpiritIndex))
            {
                spiritAliveStatus[currentSpiritIndex] = false;
            }

            // 当前Spirit死亡，尝试切换到下一个存活的Spirit
            if (TrySwitchToNextAliveSpirit())
            {
                Debug.Log($"BattleController: Current spirit defeated. Switching to next spirit.");
                // 切换成功，继续战斗
                if (battleView != null)
                    battleView.Refresh();
                if (spiritSwitcher != null)
                    spiritSwitcher.RefreshSlots();
                if (effectDisplay != null)
                    effectDisplay.RefreshDisplay();
            }
            else
            {
                // 所有Spirit都死亡，战斗失败
                State = BattleState.Defeat;
                Debug.Log("BattleController: All spirits defeated. Battle lost.");
            }
        }
    }

    /// <summary>
    /// 手动切换到指定索引的Spirit
    /// </summary>
    /// <param name="spiritIndex">目标Spirit索引（0-based）</param>
    /// <returns>是否切换成功</returns>
    public bool SwitchToSpirit(int spiritIndex)
    {
        // 验证索引
        if (spiritIndex < 0 || spiritIndex >= spiritQueue.Count)
        {
            Debug.LogWarning($"BattleController: Invalid spirit index {spiritIndex}");
            return false;
        }

        // 不能切换到当前Spirit
        if (spiritIndex == currentSpiritIndex)
        {
            Debug.Log($"BattleController: Spirit {spiritIndex} is already active");
            return false;
        }

        // 不能切换到死亡的Spirit
        if (!IsSpiritAlive(spiritIndex))
        {
            Debug.LogWarning(
                $"BattleController: Cannot switch to dead spirit at index {spiritIndex}"
            );
            return false;
        }

        // 执行切换
        return PerformSpiritSwitch(spiritIndex);
    }

    /// <summary>
    /// 尝试切换到下一个存活的Spirit（自动切换）
    /// </summary>
    /// <returns>是否切换成功</returns>
    private bool TrySwitchToNextAliveSpirit()
    {
        // 从当前索引的下一个开始查找存活的Spirit
        for (int i = currentSpiritIndex + 1; i < spiritQueue.Count; i++)
        {
            if (IsSpiritAlive(i))
            {
                return PerformSpiritSwitch(i);
            }
        }

        // 没有找到存活的Spirit
        return false;
    }

    /// <summary>
    /// 尝试切换到下一个Spirit
    /// </summary>
    /// <returns>是否切换成功</returns>
    private bool TrySwitchToNextSpirit()
    {
        // 检查是否还有下一个Spirit
        if (currentSpiritIndex + 1 >= spiritQueue.Count)
        {
            return false; // 没有下一个Spirit了
        }

        // 切换到下一个Spirit
        return PerformSpiritSwitch(currentSpiritIndex + 1);
    }

    /// <summary>
    /// 执行Spirit切换
    /// </summary>
    private bool PerformSpiritSwitch(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= spiritQueue.Count)
            return false;

        // 保存当前Spirit的运行时数据
        if (player != null && spiritRuntimeData.ContainsKey(currentSpiritIndex))
        {
            spiritRuntimeData[currentSpiritIndex] = new SpiritRuntimeData
            {
                CurrentHP = player.HP,
                MaxHP = player.MaxHP,
                CurrentMP = player.Mana,
                MaxMP = player.MaxMana,
            };
        }

        // 保存旧的Spirit索引
        int oldIndex = currentSpiritIndex;

        // 切换索引
        currentSpiritIndex = targetIndex;
        var nextSpiritData = spiritQueue[currentSpiritIndex];
        player = new Spirit(nextSpiritData);

        // 恢复目标Spirit的运行时数据
        if (spiritRuntimeData.ContainsKey(targetIndex))
        {
            var runtimeData = spiritRuntimeData[targetIndex];
            // 设置HP和MP为之前保存的值
            int hpLoss = player.MaxHP - runtimeData.CurrentHP;
            if (hpLoss > 0)
            {
                player.ReceiveDamage(hpLoss);
            }

            int manaLoss = player.MaxMana - runtimeData.CurrentMP;
            if (manaLoss > 0)
            {
                player.ConsumeMana(manaLoss);
            }
        }

        Debug.Log(
            $"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName}"
        );

        // 更新BattleModel中的玩家单位
        model.UpdatePlayer(player);

        // 重新应用全队羁绊到新的Spirit上
        model.UpdateTeamSynergiesOwner();
        Debug.Log("BattleController: Team synergies re-applied to new spirit");

        // 重置缓存值
        lastPlayerHP = player?.HP ?? 0;
        lastPlayerMana = player?.Mana ?? 0;

        // 刷新UI
        if (battleView != null)
            battleView.Refresh();
        if (spiritSwitcher != null)
            spiritSwitcher.RefreshSlots();
        if (effectDisplay != null)
            effectDisplay.RefreshDisplay();

        return true;
    }

    private void Update()
    {
        // 监测玩家/敌方 HP 或 Mana 的变化；若变化则刷新 UI（不修改 Spirit/Enemy 源码）
        bool changed = false;
        if (player != null)
        {
            if (player.HP != lastPlayerHP || player.Mana != lastPlayerMana)
            {
                lastPlayerHP = player.HP;
                lastPlayerMana = player.Mana;
                changed = true;

                // 更新当前Spirit的运行时数据
                if (spiritRuntimeData != null && spiritRuntimeData.ContainsKey(currentSpiritIndex))
                {
                    spiritRuntimeData[currentSpiritIndex] = new SpiritRuntimeData
                    {
                        CurrentHP = player.HP,
                        MaxHP = player.MaxHP,
                        CurrentMP = player.Mana,
                        MaxMP = player.MaxMana,
                    };
                }

                // 刷新Spirit切换器（显示实时HP/MP）
                if (spiritSwitcher != null)
                    spiritSwitcher.RefreshSlots();
            }
        }

        if (enemy != null)
        {
            if (enemy.HP != lastEnemyHP || enemy.Mana != lastEnemyMana)
            {
                lastEnemyHP = enemy.HP;
                lastEnemyMana = enemy.Mana;
                changed = true;
            }
        }

        if (changed && battleView != null)
            battleView.Refresh();
    }

    /// <summary>
    /// 触发狂战士羁绊的怒意机制
    /// 在每次释放技能后调用，检查并应用怒意效果
    /// </summary>
    private void TriggerBerserkerRage()
    {
        if (player == null || enemy == null)
            return;

        // 检查玩家是否有怒意Buff
        var rageBuff = Berserker.CurrentRageBuff;
        if (rageBuff != null && rageBuff.Owner == player)
        {
            // 检查并应用怒意效果（积累或消耗）
            rageBuff.CheckAndApplyRage(enemy);
        }
    }

    /// <summary>
    /// 触发疗愈者羁绊效果（在使用技能后调用）
    /// </summary>
    private void TriggerHealerSynergy()
    {
        if (player == null || model == null)
            return;

        // 检查玩家是否有疗愈者Buff
        var buffs = model.GetBuffsForUnit(player);
        foreach (var buff in buffs)
        {
            if (buff is HealerBuff healerBuff)
            {
                // 触发治疗效果
                healerBuff.TriggerHeal(model);
                break; // 只触发一次
            }
        }
    }
}
