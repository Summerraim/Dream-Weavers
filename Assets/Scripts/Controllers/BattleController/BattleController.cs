using UnityEngine;

public enum BattleState
{
    None,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat,
}

/// <summary>
/// 战斗输入状态（用于道具使用流程）
/// </summary>
public enum BattleInputState
{
    Normal,              // 正常状态（可以使用技能、结束回合）
    WaitingForItemTarget // 等待选择道具目标
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

    [Header("UI技能动画")]
    [SerializeField]
    private Animator playerSkillAnimator; // 玩家技能动画播放器

    [SerializeField]
    private Animator enemySkillAnimator; // 敌人技能动画播放器

    [SerializeField]
    private DreamWeavers.Rooms.CombatRoom_cza combatRoom; // 战斗房间引用（用于捕捉精灵）

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

    // 道具使用状态机
    private BattleInputState inputState = BattleInputState.Normal;
    private ItemData pendingItem; // 待使用的道具

    private void Start()
    {
        // 禁用技能动画的Root Motion（防止动画影响物体位置）
        if (playerSkillAnimator != null)
            playerSkillAnimator.applyRootMotion = false;
        if (enemySkillAnimator != null)
            enemySkillAnimator.applyRootMotion = false;

        // 场景加载时自动初始化战斗
        InitializeBattle();
        Debug.Log("BattleController: InitializeBattle called in Start()");
    }

    // 允许外部（如房间控制器）传入玩家/敌人数据并开始战斗
    // public void BeginBattleWith(PlayerData player, EnemyData enemy)
    // {
    //     if (player == null || enemy == null)
    //     {
    //         Debug.LogWarning("BattleController.BeginBattleWith: player or enemy data is null");
    //         return;
    //     }
    //     playerData = player;
    //     enemyData = enemy;
    //     InitializeBattle();
    // }

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
        // 如果是首次初始化，创建新字典；否则保留现有数据
        if (spiritAliveStatus == null)
        {
            spiritAliveStatus = new System.Collections.Generic.Dictionary<int, bool>();
        }

        if (spiritRuntimeData == null)
        {
            spiritRuntimeData = new System.Collections.Generic.Dictionary<int, SpiritRuntimeData>();
        }

        for (int i = 0; i < spiritQueue.Count; i++)
        {
            // 恢复存活状态（如果之前死亡，保持死亡；否则存活）
            if (!spiritAliveStatus.ContainsKey(i))
            {
                spiritAliveStatus[i] = true;
            }

            // 初始化运行时数据（仅在首次初始化时设置为满血满蓝）
            if (!spiritRuntimeData.ContainsKey(i))
            {
                var data = spiritQueue[i];
                spiritRuntimeData[i] = new SpiritRuntimeData
                {
                    CurrentHP = data.MaxHP,
                    MaxHP = data.MaxHP,
                    CurrentMP = data.MaxMana,
                    MaxMP = data.MaxMana,
                };
                Debug.Log(
                    $"BattleController: Spirit {i} ({data.DisplayName}) initialized with full HP/MP"
                );
            }
            else
            {
                Debug.Log(
                    $"BattleController: Spirit {i} retaining previous HP/MP: {spiritRuntimeData[i].CurrentHP}/{spiritRuntimeData[i].MaxHP} HP, {spiritRuntimeData[i].CurrentMP}/{spiritRuntimeData[i].MaxMP} MP"
                );
            }
        }

        // 创建第一个Spirit
        player = new Spirit(spiritQueue[currentSpiritIndex]);

        // 恢复第一个Spirit的运行时数据（HP/MP）
        if (spiritRuntimeData.ContainsKey(currentSpiritIndex))
        {
            var runtimeData = spiritRuntimeData[currentSpiritIndex];
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

            Debug.Log(
                $"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName} (HP: {player.HP}/{player.MaxHP}, MP: {player.Mana}/{player.MaxMana})"
            );
        }
        else
        {
            Debug.Log(
                $"BattleController: Spirit {currentSpiritIndex + 1}/{spiritQueue.Count} entering battle: {player.DisplayName}"
            );
        }

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
        SacrificeSynergyBridge.IsSpiritAliveAtIndex = IsSpiritAlive;

        // 初始化全队羁绊系统（战斗开始时统计所有出场Spirit的Synergy并应用效果）
        model.InitializeTeamSynergies(spiritQueue);
        Debug.Log("BattleController: Team synergies initialized");

        // 绑定InventoryManager到战斗上下文
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.BindBattle(model);
            Debug.Log("BattleController: InventoryManager bound to battle");
        }

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

    /// <summary>
    /// 重置所有Spirit的HP/MP为满值（用于游戏开始或重生）
    /// </summary>
    public void ResetAllSpiritsToFull()
    {
        if (spiritQueue == null || spiritRuntimeData == null)
        {
            Debug.LogWarning(
                "BattleController: Cannot reset spirits - no spirit queue or runtime data"
            );
            return;
        }

        for (int i = 0; i < spiritQueue.Count; i++)
        {
            var data = spiritQueue[i];
            spiritRuntimeData[i] = new SpiritRuntimeData
            {
                CurrentHP = data.MaxHP,
                MaxHP = data.MaxHP,
                CurrentMP = data.MaxMana,
                MaxMP = data.MaxMana,
            };

            // 如果是死亡的Spirit，恢复为存活
            if (spiritAliveStatus != null && spiritAliveStatus.ContainsKey(i))
            {
                spiritAliveStatus[i] = true;
            }
        }

        Debug.Log("BattleController: All spirits reset to full HP/MP");
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

        // 启动协程执行技能（包含动画）
        StartCoroutine(ExecuteSkillWithAnimation(skill, skillIndex, player, enemy, true));
    }

    /// <summary>
    /// 执行技能并播放动画（协程）
    /// </summary>
    private System.Collections.IEnumerator ExecuteSkillWithAnimation(
        ISkill skill,
        int skillIndex,
        IBattleUnit caster,
        IBattleUnit target,
        bool isPlayerSkill
    )
    {
        Debug.Log(
            $"BattleController: {caster.DisplayName} using skill: {skill.DisplayName}, ManaCost={skill.ManaCost}"
        );

        // 扣除蓝量
        if (caster is Spirit spirit)
        {
            spirit.ConsumeMana(skill.ManaCost);
            Debug.Log($"BattleController: Mana consumed. Remaining: {spirit.Mana}");
        }
        else if (caster is Enemy enemyUnit)
        {
            enemyUnit.ConsumeMana(skill.ManaCost);
        }

        // 如果有动画，播放动画并等待
        if (skill.SkillAnimation != null)
        {
            Debug.Log($"BattleController: Playing animation for {skill.DisplayName}");

            // 确定使用哪个Animator
            Animator targetAnimator = (caster == player) ? playerSkillAnimator : enemySkillAnimator;

            if (targetAnimator != null)
            {
                // 激活动画GameObject
                targetAnimator.gameObject.SetActive(true);

                // 使用AnimatorOverrideController在运行时替换动画
                AnimatorOverrideController overrideController;

                if (targetAnimator.runtimeAnimatorController is AnimatorOverrideController existing)
                {
                    // 如果已经有OverrideController，复用它
                    overrideController = existing;
                }
                else
                {
                    // 创建新的OverrideController
                    if (targetAnimator.runtimeAnimatorController == null)
                    {
                        Debug.LogError($"BattleController: {(caster == player ? "Player" : "Enemy")} Skill Animator has no AnimatorController! Please create a basic AnimatorController with a 'Skill' state and assign it.");
                        targetAnimator.gameObject.SetActive(false);
                        yield return new WaitForSeconds(skill.SkillAnimation.length);
                        yield break;
                    }

                    overrideController = new AnimatorOverrideController(targetAnimator.runtimeAnimatorController);
                    targetAnimator.runtimeAnimatorController = overrideController;
                }

                // 替换所有动画为当前技能的动画
                var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);

                for (int i = 0; i < overrides.Count; i++)
                {
                    overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(
                        overrides[i].Key,
                        skill.SkillAnimation
                    );
                }

                overrideController.ApplyOverrides(overrides);

                // 播放动画（假设状态名为 "Skill"）
                targetAnimator.Play("Skill", 0, 0f);

                Debug.Log(
                    $"BattleController: Playing animation '{skill.SkillAnimation.name}' on {(caster == player ? "Player" : "Enemy")} animator, duration: {skill.SkillAnimation.length}s"
                );

                // 等待动画播放完成
                yield return new WaitForSeconds(skill.SkillAnimation.length);

                Debug.Log($"BattleController: Animation finished");

                // 禁用动画GameObject
                targetAnimator.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning(
                    $"BattleController: Skill animator not set! Please assign '{(caster == player ? "Player" : "Enemy")} Skill Animator' in Inspector."
                );
                // 即使没有animator，也等待动画时长（保持节奏）
                yield return new WaitForSeconds(skill.SkillAnimation.length);
            }
        }

        // 执行技能效果
        Debug.Log($"BattleController: Executing skill effects...");
        skill.Execute(caster, target);
        Debug.Log($"BattleController: Target HP after skill: {target.HP}");

        // 刷新UI显示血条变化
        if (battleView != null)
            battleView.Refresh();

        // 记录技能使用次数和冷却（玩家和敌人都需要）
        if (skillIndex >= 0 && model != null)
        {
            model.IncrementSkillUsage(skillIndex);

            // 设置冷却
            if (skill.CooldownTurns > 0)
            {
                model.SetSkillCooldown(skillIndex, skill.CooldownTurns);
                Debug.Log(
                    $"BattleController: {(isPlayerSkill ? "Player" : "Enemy")} skill {skillIndex} set on cooldown for {skill.CooldownTurns} turns"
                );
            }
        }

        // 仅玩家技能触发这些羁绊效果
        if (isPlayerSkill)
        {
            // 触发狂战士羁绊的怒意机制（如果存在）
            TriggerBerserkerRage();

            // 触发疗愈者羁绊效果（如果存在）
            TriggerHealerSynergy();
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
        StartCoroutine(EnemyActCoroutine());

        if (battleView != null)
            battleView.Refresh();
    }

    private System.Collections.IEnumerator EnemyActCoroutine()
    {
        if (enemyAI == null || enemy == null || player == null)
        {
            State = BattleState.PlayerTurn;
            yield break;
        }

        // AI决定使用的技能（传入BattleModel以检查冷却和使用次数）
        int skillIndex;
        var skill = enemyAI.DecideSkill(enemy, player, model, out skillIndex);
        if (skill == null)
        {
            Debug.Log("AIController: Enemy has no available skills");
            State = BattleState.PlayerTurn;
            yield break;
        }

        // 使用偏移后的索引来存储敌人技能的冷却和使用次数
        int enemySkillIndex = AIController.GetEnemySkillIndex(skillIndex);

        // 使用协程执行技能（包含动画）
        yield return StartCoroutine(
            ExecuteSkillWithAnimation(skill, enemySkillIndex, enemy, player, false)
        );

        // 敌人回合结束
        if (State == BattleState.EnemyTurn)
            State = BattleState.PlayerTurn;

        if (battleView != null)
            battleView.Refresh();
    }

    private void UpdateBattleStateAfterAction()
    {
        // 检查狩猎大师羁绊的诱捕条件（在敌人完全死亡前）
        if (player != null && enemy != null && !enemy.IsDead && model != null && combatRoom != null)
        {
            // 检查玩家是否有诱捕Buff
            var buffs = model.GetBuffsForUnit(player);
            foreach (var buff in buffs)
            {
                if (buff is HunterMasterBuff hunterBuff)
                {
                    // 检查是否满足诱捕条件
                    if (hunterBuff.CanCaptureEnemy(enemy))
                    {
                        State = BattleState.Victory;
                        Debug.Log(
                            "BattleController: HunterMaster trap activated! Enemy can be captured early."
                        );

                        // 显示敌人死亡后的面板
                        if (battleView != null)
                        {
                            battleView.ShowEnemyDeathPanel();
                        }

                        // 立即尝试捕捉精灵（提前捕捉）
                        var (success, capturedSpirit) = combatRoom.AttemptCapture(
                            earlyCapture: true
                        );
                        if (battleView != null)
                        {
                            if (success && capturedSpirit != null)
                            {
                                battleView.ShowCaptureSuccess(capturedSpirit.DisplayName);
                            }
                            else
                            {
                                battleView.ShowCaptureFailed();
                            }
                        }

                        return;
                    }
                    break;
                }
            }
        }

        if (enemy != null && enemy.IsDead)
        {
            State = BattleState.Victory;
            Debug.Log("BattleController: Enemy defeated. Victory!");

            // 显示敌人死亡后的面板
            if (battleView != null)
            {
                battleView.ShowEnemyDeathPanel();
            }

            // 立即尝试捕捉精灵
            if (combatRoom != null)
            {
                var (success, capturedSpirit) = combatRoom.AttemptCapture();
                if (battleView != null)
                {
                    if (success && capturedSpirit != null)
                    {
                        battleView.ShowCaptureSuccess(capturedSpirit.DisplayName);
                    }
                    else
                    {
                        battleView.ShowCaptureFailed();
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    "BattleController: combatRoom reference is null, cannot attempt capture"
                );
            }

            return;
        }

        if (player != null && player.IsDead)
        {
            // 标记当前Spirit为死亡
            if (spiritAliveStatus != null && spiritAliveStatus.ContainsKey(currentSpiritIndex))
            {
                spiritAliveStatus[currentSpiritIndex] = false;
            }

            // 检查是否还有存活的Spirit
            bool hasAliveSpirit = false;
            if (spiritAliveStatus != null)
            {
                foreach (var status in spiritAliveStatus.Values)
                {
                    if (status)
                    {
                        hasAliveSpirit = true;
                        break;
                    }
                }
            }

            if (hasAliveSpirit)
            {
                // 还有存活的Spirit，打开Spirit切换面板让玩家手动选择
                Debug.Log(
                    $"BattleController: Current spirit defeated. Opening spirit switcher panel."
                );
                if (battleView != null)
                {
                    battleView.ShowSpiritSwitcherPanel();
                    battleView.Refresh();
                }
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

    #region 道具使用系统

    /// <summary>
    /// 由UI_InventoryView调用：玩家请求使用道具
    /// </summary>
    public void OnItemUseRequested(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("BattleController: OnItemUseRequested called with null item");
            return;
        }

        if (State != BattleState.PlayerTurn)
        {
            Debug.LogWarning("BattleController: Cannot use item, not player turn");
            return;
        }

        if (inputState != BattleInputState.Normal)
        {
            Debug.LogWarning("BattleController: Already waiting for item target selection");
            return;
        }

        Debug.Log($"BattleController: Item use requested: {item.DisplayName}, TargetMode={item.TargetMode}");

        // 根据TargetingMode决定流程
        switch (item.TargetMode)
        {
            case TargetingMode.Self:
                // 直接对自己使用
                UseItemOnTarget(item, player);
                break;

            case TargetingMode.SingleUnit:
                // 进入目标选择状态
                inputState = BattleInputState.WaitingForItemTarget;
                pendingItem = item;

                // 激活Spirit切换器，提示玩家选择目标
                if (battleView != null)
                {
                    battleView.ShowSpiritSwitcherForItemTarget();
                    Debug.Log("BattleController: Waiting for player to select a Spirit as target");
                }
                else
                {
                    Debug.LogError("BattleController: UI_BattleView not assigned! Please assign it in Inspector.");
                    // 回退状态
                    inputState = BattleInputState.Normal;
                    pendingItem = null;
                }
                break;

            case TargetingMode.AllAllies:
            case TargetingMode.AllEnemies:
            case TargetingMode.AllUnits:
                // 群体效果，target传null，由Effect内部处理
                UseItemOnTarget(item, null);
                break;

            default:
                Debug.LogWarning($"BattleController: Unhandled TargetingMode: {item.TargetMode}");
                break;
        }
    }

    /// <summary>
    /// 由UI_BattleView调用：玩家选择了一个Spirit作为道具目标
    /// </summary>
    public void OnSpiritSelectedAsItemTarget(int spiritIndex)
    {
        if (inputState != BattleInputState.WaitingForItemTarget)
        {
            Debug.LogWarning("BattleController: OnSpiritSelectedAsItemTarget called but not waiting for target");
            return;
        }

        if (pendingItem == null)
        {
            Debug.LogError("BattleController: pendingItem is null");
            inputState = BattleInputState.Normal;
            return;
        }

        // 获取选中的Spirit作为目标
        IBattleUnit target = GetSpiritAsTarget(spiritIndex);

        if (target == null)
        {
            Debug.LogWarning($"BattleController: Invalid target Spirit at index {spiritIndex}");
            // 不重置状态，让玩家重新选择
            return;
        }

        Debug.Log($"BattleController: Spirit {spiritIndex} selected as item target: {target.DisplayName}");

        // 使用道具
        UseItemOnTarget(pendingItem, target, spiritIndex);

        // 重置状态
        inputState = BattleInputState.Normal;
        pendingItem = null;

        // 关闭Spirit切换器
        if (battleView != null)
        {
            battleView.HideSpiritSwitcherPanel();
        }
    }

    /// <summary>
    /// 取消道具目标选择
    /// </summary>
    public void CancelItemTargetSelection()
    {
        if (inputState == BattleInputState.WaitingForItemTarget)
        {
            inputState = BattleInputState.Normal;
            pendingItem = null;

            Debug.Log("BattleController: Item target selection cancelled");

            // 关闭Spirit切换器
            if (battleView != null)
            {
                battleView.HideSpiritSwitcherPanel();
            }
        }
    }

    /// <summary>
    /// 获取当前输入状态（用于UI判断）
    /// </summary>
    public BattleInputState GetInputState()
    {
        return inputState;
    }

    /// <summary>
    /// 实际执行道具使用
    /// </summary>
    private void UseItemOnTarget(ItemData item, IBattleUnit target, int targetSpiritIndex = -1)
    {
        if (item == null)
            return;

        // 使用者默认为当前玩家Spirit
        IBattleUnit user = player;

        // 检查是否可以使用
        if (!item.CanUse(user, target))
        {
            Debug.LogWarning($"BattleController: Cannot use item {item.DisplayName}");
            return;
        }

        Debug.Log($"BattleController: Using item {item.DisplayName} on {(target != null ? target.DisplayName : "null (群体)")}");
        Debug.Log($"BattleController: Target HP before use: {(target != null ? target.HP + "/" + target.MaxHP : "N/A")}");

        // 通过InventoryManager使用道具（直接传递ItemData）
        if (InventoryManager.Instance != null)
        {
            Debug.Log($"BattleController: Calling InventoryManager.UseItem with ItemData={item.DisplayName}");
            InventoryManager.Instance.UseItem(item, user, target);
        }
        else
        {
            // 如果没有InventoryManager，直接调用道具的Use方法
            Debug.Log($"BattleController: InventoryManager not found, calling item.Use directly");
            item.Use(user, target);
        }

        Debug.Log($"BattleController: Target HP after use: {(target != null ? target.HP + "/" + target.MaxHP : "N/A")}");

        // 如果目标是非当前Spirit，保存使用后的数据
        if (targetSpiritIndex >= 0 && targetSpiritIndex != currentSpiritIndex && target is Spirit targetSpirit)
        {
            Debug.Log($"BattleController: Saving runtime data for Spirit {targetSpiritIndex}");
            SaveSpiritRuntimeDataAfterItem(targetSpiritIndex, targetSpirit);
        }
        else if (targetSpiritIndex >= 0)
        {
            Debug.Log($"BattleController: Not saving (index={targetSpiritIndex}, current={currentSpiritIndex}, target is Spirit: {target is Spirit})");
        }

        // 刷新UI
        if (battleView != null)
            battleView.Refresh();

        Debug.Log($"BattleController: Item {item.DisplayName} used successfully");
    }

    /// <summary>
    /// 根据索引获取Spirit作为目标（支持选择非当前上场的Spirit）
    /// </summary>
    private IBattleUnit GetSpiritAsTarget(int spiritIndex)
    {
        // 如果是当前Spirit，直接返回
        if (spiritIndex == currentSpiritIndex && player != null)
        {
            return player;
        }

        // 否则，创建临时Spirit实例作为目标
        if (spiritIndex >= 0 && spiritIndex < spiritQueue.Count)
        {
            var spiritData = spiritQueue[spiritIndex];
            var tempSpirit = new Spirit(spiritData);

            // 恢复该Spirit的运行时数据（HP/MP）
            if (spiritRuntimeData.ContainsKey(spiritIndex))
            {
                var runtimeData = spiritRuntimeData[spiritIndex];
                int hpLoss = tempSpirit.MaxHP - runtimeData.CurrentHP;
                if (hpLoss > 0)
                {
                    tempSpirit.ReceiveDamage(hpLoss);
                }

                int manaLoss = tempSpirit.MaxMana - runtimeData.CurrentMP;
                if (manaLoss > 0)
                {
                    tempSpirit.ConsumeMana(manaLoss);
                }
            }

            Debug.Log($"BattleController: Created temp Spirit for index {spiritIndex}: HP={tempSpirit.HP}/{tempSpirit.MaxHP}, MP={tempSpirit.Mana}/{tempSpirit.MaxMana}");

            // 注意：不在这里保存，而是在道具使用后保存
            return tempSpirit;
        }

        return null;
    }

    /// <summary>
    /// 使用道具后，保存Spirit的运行时数据
    /// </summary>
    private void SaveSpiritRuntimeDataAfterItem(int spiritIndex, Spirit spirit)
    {
        if (spirit == null || spiritRuntimeData == null)
            return;

        spiritRuntimeData[spiritIndex] = new SpiritRuntimeData
        {
            CurrentHP = spirit.HP,
            MaxHP = spirit.MaxHP,
            CurrentMP = spirit.Mana,
            MaxMP = spirit.MaxMana,
        };

        Debug.Log($"BattleController: Saved Spirit {spiritIndex} runtime data after item use: HP={spirit.HP}/{spirit.MaxHP}, MP={spirit.Mana}/{spirit.MaxMana}");
    }

    #endregion
}
