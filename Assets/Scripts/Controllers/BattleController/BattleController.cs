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

    private BattleModel model;

    // Spirit队列系统
    private System.Collections.Generic.List<SpiritData> spiritQueue;
    private int currentSpiritIndex = 0;

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

        // 创建第一个Spirit
        player = new Spirit(spiritQueue[currentSpiritIndex]);
        Debug.Log($"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName}");

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

        // 设置Debuff系统的静态引用
        WeakenAttack.CurrentBattle = model;
        WeakenDefense.CurrentBattle = model;
        ManaLeech.CurrentBattle = model;
        HealingReduction.CurrentBattle = model;
        Vulnerability.CurrentBattle = model;
        Poison.CurrentBattle = model;
        Blind.CurrentBattle = model;

        // 绑定 UI（如果存在）
        if (battleView != null)
            battleView.Bind(this, model);

        // 初始化缓存值
        lastPlayerHP = player?.HP ?? 0;
        lastPlayerMana = player?.Mana ?? 0;
        lastEnemyHP = enemy?.HP ?? 0;
        lastEnemyMana = enemy?.Mana ?? 0;

        State = BattleState.PlayerTurn;
    }

    public Spirit Player => player;
    public Enemy Enemy => enemy;

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
            // 当前Spirit死亡，尝试切换到下一个
            if (TrySwitchToNextSpirit())
            {
                Debug.Log($"BattleController: Current spirit defeated. Switching to next spirit.");
                // 切换成功，继续战斗
                if (battleView != null)
                    battleView.Refresh();
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
        currentSpiritIndex++;
        var nextSpiritData = spiritQueue[currentSpiritIndex];
        player = new Spirit(nextSpiritData);

        Debug.Log($"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName}");

        // 更新BattleModel中的玩家单位
        model.UpdatePlayer(player);

        // 重置缓存值
        lastPlayerHP = player?.HP ?? 0;
        lastPlayerMana = player?.Mana ?? 0;

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
}
