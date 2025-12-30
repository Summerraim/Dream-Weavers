using System.Collections.Generic;
using DreamWeavers.Rooms;
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
    Normal, // 正常状态（可以使用技能、结束回合）
    WaitingForItemTarget, // 等待选择道具目标
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

    [SerializeField]
    private DreamWeavers.Rooms.BossRoom_cza bossRoom; // Boss房间引用

    private BattleModel model;

    // Spirit队列系统
    private System.Collections.Generic.List<SpiritData> spiritQueue;
    private int currentSpiritIndex = 0;
    private System.Collections.Generic.Dictionary<SpiritData, bool> spiritAliveStatus; // 跟踪每个Spirit的存活状态（按SpiritData）
    private System.Collections.Generic.Dictionary<SpiritData, SpiritRuntimeData> spiritRuntimeData; // 跟踪每个Spirit的运行时数据（HP/MP）（按SpiritData）
    private bool isForcedSwitchDueToDefeat = false; // 标记是否因心兽死亡而强制切换

    // 缓存的 HP/Mana 值，用于检测变化
    private int lastPlayerHP;
    private int lastPlayerMana;
    private int lastEnemyHP;
    private int lastEnemyMana;

    public BattleState State { get; private set; } = BattleState.None;

    // 提供UI层读取序列化的PlayerData（只读）
    public PlayerData PlayerData => playerData;

    // 道具使用状态机
    private BattleInputState inputState = BattleInputState.Normal;
    private ItemData pendingItem; // 待使用的道具

    [Header("初始化设置")]
    [Tooltip("是否在Start时自动初始化战斗（建议关闭，由CombatRoom传入数据后初始化）")]
    [SerializeField]
    private bool autoInitOnStart = false;

    private void Start()
    {
        // 禁用技能动画的Root Motion（防止动画影响物体位置）
        if (playerSkillAnimator != null)
            playerSkillAnimator.applyRootMotion = false;
        if (enemySkillAnimator != null)
            enemySkillAnimator.applyRootMotion = false;

        // 仅当启用自动初始化时才在Start中初始化战斗
        // 正常流程应由CombatRoom/BossRoom调用BeginBattleWith传入数据后初始化
        if (autoInitOnStart)
        {
            InitializeBattle();
            Debug.Log(
                "BattleController: InitializeBattle called in Start() (autoInitOnStart=true)"
            );
        }
        // 不输出等待日志，避免混淆
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
        Debug.Log(
            $"BattleController: InitializeBattle starting - enemyData={(enemyData != null ? enemyData.name : "null")}, MaxHP={(enemyData != null ? enemyData.MaxHP.ToString() : "N/A")}"
        );
        Debug.Log(
            $"BattleController: InitializeBattle - 当前房间引用状态: combatRoom={(combatRoom != null ? combatRoom.gameObject.name : "null")}, bossRoom={(bossRoom != null ? bossRoom.gameObject.name : "null")}"
        );

        // 从PlayerData获取出场的Spirit队列
        if (playerData == null)
        {
            Debug.LogError("BattleController: PlayerData is null!");
            return;
        }

        if (enemyData == null)
        {
            Debug.LogError(
                "BattleController: EnemyData is null! Cannot initialize battle without enemy."
            );
            return;
        }

        // 优先从PlayerManager获取最新的部署Spirit列表
        if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayer != null)
        {
            spiritQueue = PlayerManager.Instance.GetDeployedSpirits();
            Debug.Log(
                $"BattleController: 从PlayerManager获取部署Spirit列表: {spiritQueue.Count} 个"
            );
        }
        else
        {
            // 降级方案：从PlayerData获取
            spiritQueue = playerData.GetDeployedSpirits();
            Debug.Log(
                $"BattleController: 从PlayerData获取部署Spirit列表: {(spiritQueue != null ? spiritQueue.Count : 0)} 个"
            );
        }

        if (spiritQueue == null || spiritQueue.Count == 0)
        {
            Debug.LogError("BattleController: No spirits deployed!");
            return;
        }

        // 查找第一个存活的心兽作为初始上场心兽
        currentSpiritIndex = -1;

        // 初始化Spirit存活状态和运行时数据
        // 如果是首次初始化，创建新字典；否则保留现有数据
        if (spiritAliveStatus == null)
        {
            spiritAliveStatus = new System.Collections.Generic.Dictionary<SpiritData, bool>();
        }

        if (spiritRuntimeData == null)
        {
            spiritRuntimeData = new System.Collections.Generic.Dictionary<SpiritData, SpiritRuntimeData>();
        }

        for (int i = 0; i < spiritQueue.Count; i++)
        {
            var spiritData = spiritQueue[i];

            // 恢复存活状态（如果之前死亡，保持死亡；否则存活）
            if (!spiritAliveStatus.ContainsKey(spiritData))
            {
                spiritAliveStatus[spiritData] = true;
            }

            // 初始化运行时数据（仅在首次初始化时设置为满血满蓝）
            if (!spiritRuntimeData.ContainsKey(spiritData))
            {
                spiritRuntimeData[spiritData] = new SpiritRuntimeData
                {
                    CurrentHP = spiritData.MaxHP,
                    MaxHP = spiritData.MaxHP,
                    CurrentMP = spiritData.MaxMana,
                    MaxMP = spiritData.MaxMana,
                };
                Debug.Log(
                    $"BattleController: Spirit {i} ({spiritData.DisplayName}) initialized with full HP/MP"
                );
            }
            else
            {
                Debug.Log(
                    $"BattleController: Spirit {i} ({spiritData.DisplayName}) retaining previous HP/MP: {spiritRuntimeData[spiritData].CurrentHP}/{spiritRuntimeData[spiritData].MaxHP} HP, {spiritRuntimeData[spiritData].CurrentMP}/{spiritRuntimeData[spiritData].MaxMP} MP"
                );
            }

            // 找到第一个存活的心兽（HP > 0）
            if (currentSpiritIndex == -1 && spiritRuntimeData.ContainsKey(spiritData))
            {
                if (spiritRuntimeData[spiritData].CurrentHP > 0)
                {
                    currentSpiritIndex = i;
                    Debug.Log($"BattleController: Found first alive spirit at index {i} ({spiritData.DisplayName})");
                }
            }
        }

        // 如果没有找到存活的心兽，回退到索引0
        if (currentSpiritIndex == -1)
        {
            Debug.LogWarning("BattleController: No alive spirit found, defaulting to index 0");
            currentSpiritIndex = 0;
        }

        // 先创建BattleModel（但不初始化），以便查询保存的Spirit状态
        if (model == null)
        {
            model = new BattleModel();
        }

        // 新战斗开始时，重置所有技能冷却和使用次数
        model.ResetSkillCooldownsAndUsage();

        // 创建第一个Spirit（使用保存的技能列表，如果存在）
        var firstSpiritData = spiritQueue[currentSpiritIndex];
        SpiritBattleState savedState = model.GetSpiritState(firstSpiritData);
        if (savedState != null && savedState.SelectedSkills.Count > 0)
        {
            player = new Spirit(firstSpiritData, savedState.SelectedSkills);
        }
        else
        {
            player = new Spirit(firstSpiritData); // 首次出场，随机选择技能
        }

        // 恢复第一个Spirit的运行时数据（HP/MP）
        var firstSpirit = spiritQueue[currentSpiritIndex];
        if (spiritRuntimeData.ContainsKey(firstSpirit))
        {
            var runtimeData = spiritRuntimeData[firstSpirit];
            player.SetRuntimeHPMP(runtimeData.CurrentHP, runtimeData.CurrentMP);

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
        Debug.Log(
            $"BattleController: Enemy created with full stats - HP: {enemy.HP}/{enemy.MaxHP}, Mana: {enemy.Mana}/{enemy.MaxMana}"
        );
        enemyAI = new AIController();

        // 初始化战斗模型
        model.InitializeBattle(player, enemy);

        // 设置Buff系统的静态引用
        Strengthen.CurrentBattle = model;
        ToughSkin.CurrentBattle = model;
        HealthRegeneration.CurrentBattle = model;
        Vampiric.CurrentBattle = model;
        Thorns.CurrentBattle = model;
        Revive.CurrentBattle = model;
        Invincibility.CurrentBattle = model;
        ManaShield.CurrentBattle = model;
        Shield.CurrentBattle = model;

        // 设置Debuff系统的静态引用
        Weaken.CurrentBattle = model;
        Poison.CurrentBattle = model;

        // 设置ControlDebuff系统的静态引用
        Frozen.CurrentBattle = model;
        Sleep.CurrentBattle = model;
        Confusion.CurrentBattle = model;

        // 设置Special系统的静态引用
        PrepareEffect.CurrentBattle = model;

        // 设置净化/驱散系统的静态引用
        Cleanse.CurrentBattle = model;
        Dispel.CurrentBattle = model;

        // 设置进化效果的静态引用
        Evolution.CurrentBattle = model;

        // 设置道具效果的静态引用
        HealAll.CurrentBattle = model;
        HealAll.GetDeployedSpirits = () => spiritQueue;
        HealAll.GetSpiritRuntimeData = GetSpiritRuntimeData;
        HealAll.SaveSpiritHP = (index, currentHP, maxHP) =>
        {
            if (index >= 0 && index < spiritQueue.Count)
            {
                var spiritData = spiritQueue[index];
                if (spiritRuntimeData.ContainsKey(spiritData))
                {
                    var data = spiritRuntimeData[spiritData];
                    spiritRuntimeData[spiritData] = new SpiritRuntimeData
                    {
                        CurrentHP = currentHP,
                        MaxHP = maxHP,
                        CurrentMP = data.CurrentMP,
                        MaxMP = data.MaxMP
                    };
                }
            }
        };

        // 设置治疗效果的静态引用（用于治疗所有已部署的精灵）
        Heal.CurrentBattle = model;
        Heal.GetDeployedSpirits = () => spiritQueue;
        Heal.GetSpiritRuntimeData = GetSpiritRuntimeData;
        Heal.SaveSpiritHP = (index, currentHP, maxHP) =>
        {
            if (index >= 0 && index < spiritQueue.Count)
            {
                var spiritData = spiritQueue[index];
                if (spiritRuntimeData.ContainsKey(spiritData))
                {
                    var data = spiritRuntimeData[spiritData];
                    spiritRuntimeData[spiritData] = new SpiritRuntimeData
                    {
                        CurrentHP = currentHP,
                        MaxHP = maxHP,
                        CurrentMP = data.CurrentMP,
                        MaxMP = data.MaxMP
                    };
                }
            }
        };

        // 设置法力恢复效果的静态引用（用于恢复所有已部署精灵的法力值）
        RestoreMana.CurrentBattle = model;
        RestoreMana.GetDeployedSpirits = () => spiritQueue;
        RestoreMana.GetSpiritRuntimeData = GetSpiritRuntimeData;
        RestoreMana.SaveSpiritMP = (index, currentMP, maxMP) =>
        {
            if (index >= 0 && index < spiritQueue.Count)
            {
                var spiritData = spiritQueue[index];
                if (spiritRuntimeData.ContainsKey(spiritData))
                {
                    var data = spiritRuntimeData[spiritData];
                    spiritRuntimeData[spiritData] = new SpiritRuntimeData
                    {
                        CurrentHP = data.CurrentHP,
                        MaxHP = data.MaxHP,
                        CurrentMP = currentMP,
                        MaxMP = maxMP
                    };
                }
            }
        };

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
        if (battleView == null)
        {
            // 尝试自动查找 UI_BattleView
            battleView = FindObjectOfType<UI_BattleView>();
            if (battleView == null)
            {
                // 尝试查找未激活的
                var allViews = Resources.FindObjectsOfTypeAll<UI_BattleView>();
                if (allViews != null && allViews.Length > 0)
                {
                    battleView = allViews[0];
                    Debug.Log(
                        $"BattleController: Found inactive UI_BattleView: {battleView.gameObject.name}"
                    );
                }
            }
        }

        if (battleView != null)
        {
            // 确保 UI_BattleView 的 GameObject 是激活的
            if (!battleView.gameObject.activeInHierarchy)
            {
                Debug.Log(
                    $"BattleController: UI_BattleView GameObject is inactive, activating it..."
                );
                // 尝试激活整个父级链
                Transform current = battleView.transform;
                while (current != null)
                {
                    if (!current.gameObject.activeSelf)
                    {
                        Debug.Log(
                            $"BattleController: Activating parent: {current.gameObject.name}"
                        );
                        current.gameObject.SetActive(true);
                    }
                    current = current.parent;
                }
            }

            battleView.Bind(this, model);
            Debug.Log(
                $"BattleController: UI_BattleView bound successfully, activeInHierarchy={battleView.gameObject.activeInHierarchy}"
            );
        }
        else
        {
            Debug.LogWarning(
                "BattleController: UI_BattleView not found! Battle UI will not be displayed."
            );
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
        BeginBattleWith(playerDataOverride, enemyDataOverride, null, null);
    }

    /// <summary>
    /// Allows external systems (ex: rooms) to begin a battle with runtime data and combat room reference.
    /// </summary>
    public void BeginBattleWith(
        PlayerData playerDataOverride,
        EnemyData enemyDataOverride,
        DreamWeavers.Rooms.CombatRoom_cza room
    )
    {
        BeginBattleWith(playerDataOverride, enemyDataOverride, room, null);
    }

    /// <summary>
    /// Allows external systems (ex: rooms) to begin a battle with runtime data and boss room reference.
    /// </summary>
    public void BeginBattleWith(
        PlayerData playerDataOverride,
        EnemyData enemyDataOverride,
        DreamWeavers.Rooms.BossRoom_cza boss
    )
    {
        Debug.Log(
            $"BattleController: BeginBattleWith(BossRoom) 重载被调用, boss={(boss != null ? boss.gameObject.name : "null")}"
        );
        BeginBattleWith(playerDataOverride, enemyDataOverride, null, boss);
    }

    /// <summary>
    /// Allows external systems (ex: rooms) to begin a battle with runtime data and room references.
    /// </summary>
    public void BeginBattleWith(
        PlayerData playerDataOverride,
        EnemyData enemyDataOverride,
        DreamWeavers.Rooms.CombatRoom_cza room,
        DreamWeavers.Rooms.BossRoom_cza boss
    )
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

        // 根据传入的参数设置房间引用
        // 如果传入了 room，则使用 room 并清空 bossRoom（这是普通战斗）
        // 如果传入了 boss，则使用 boss 并清空 combatRoom（这是Boss战斗）
        // 这样避免同时存在两个房间引用导致混乱
        Debug.Log(
            $"BattleController: BeginBattleWith 接收参数 - room={(room != null ? room.gameObject.name : "null")}, boss={(boss != null ? boss.gameObject.name : "null")}"
        );

        if (room != null)
        {
            combatRoom = room;
            bossRoom = null; // 普通战斗，清空Boss引用
            Debug.Log($"BattleController: CombatRoom reference set to {room.gameObject.name}");
        }
        else if (boss != null)
        {
            bossRoom = boss;
            combatRoom = null; // Boss战斗，清空普通房间引用
            Debug.Log(
                $"BattleController: BossRoom reference set to {boss.gameObject.name}, bossRoom={(bossRoom != null ? bossRoom.gameObject.name : "NULL after assignment!")}"
            );
        }
        else
        {
            // 两个都没传，清空引用
            combatRoom = null;
            bossRoom = null;
            Debug.LogWarning("BattleController: BeginBattleWith called without room reference");
        }

        // 检查敌人是否已被击败（HP/Mana为0的情况）
        if (EnemyPool.IsEnemyDefeated(enemyDataOverride))
        {
            Debug.Log(
                $"BattleController: 敌人 {enemyDataOverride.name} 已被击败，跳过战斗直接进入下一房间"
            );
            SkipBattleAndContinue();
            return;
        }

        // 重置之前的战斗状态
        ResetBattleState();

        playerData = playerDataOverride;
        enemyData = enemyDataOverride;

        Debug.Log(
            $"BattleController: BeginBattleWith - PlayerData={playerData.name}, EnemyData={enemyData.name}, CombatRoom={(combatRoom != null ? combatRoom.gameObject.name : "null")}, BossRoom={(bossRoom != null ? bossRoom.gameObject.name : "null")}"
        );

        InitializeBattle();
    }

    /// <summary>
    /// 跳过战斗直接进入下一房间（当敌人已被击败时调用）
    /// </summary>
    private void SkipBattleAndContinue()
    {
        Debug.Log("[BattleController] SkipBattleAndContinue: 敌人已被击败，跳过战斗");

        // 标记房间已清理
        if (combatRoom != null)
        {
            combatRoom.MarkAsCleared();
        }
        else if (bossRoom != null)
        {
            bossRoom.MarkAsCleared();
        }

        // 隐藏战斗UI
        if (battleView != null)
        {
            battleView.HideBattlePanel();
        }

        // 跳过战斗也等同于“结束战斗”，避免 BattleController 在非战斗房间中一直保持激活
        EndBattleAndDeactivate();

        // 通知 RoomStateMachine 完成当前房间
        if (RoomStateMachine_cza.Instance != null)
        {
            Debug.Log("[BattleController] 通知 RoomStateMachine 完成房间");
            RoomStateMachine_cza.Instance.CompleteCurrentRoom();
        }
        else
        {
            Debug.LogWarning(
                "[BattleController] RoomStateMachine_cza.Instance 为 null，无法触发路线选择"
            );
        }
    }

    /// <summary>
    /// 重置战斗状态（在开始新战斗前调用）
    /// </summary>
    private void ResetBattleState()
    {
        // 清空当前敌人引用
        enemy = null;
        enemyAI = null;

        // 重置战斗状态
        State = BattleState.None;
        inputState = BattleInputState.Normal;
        pendingItem = null;

        Debug.Log("BattleController: Battle state reset for new battle");
    }

    /// <summary>
    /// 结束当前战斗会话并将 BattleController 设为不活动状态。
    /// - 由 UI_BattleView “继续”/RoomUI 离开战斗房间时调用
    /// - 这样 FindObjectOfType&lt;BattleController&gt;() 在非战斗房间会返回 null，避免 BattleController “始终激活”
    /// </summary>
    public void EndBattleAndDeactivate()
    {
        // 清理/解绑 UI，避免 UI 保留对上一场战斗的 controller/model 引用
        if (battleView != null)
        {
            battleView.HideEnemyDeathPanel();
            battleView.HideCapturePanel();
            battleView.HideLosePanel();
            battleView.Unbind();
        }

        // 清空当前战斗运行时引用（Spirit 的 HP/MP 已在 Update 中同步进 spiritRuntimeData）
        player = null;
        enemy = null;
        enemyAI = null;

        // 清空房间引用，避免跨房间残留
        combatRoom = null;
        bossRoom = null;

        // 重置战斗状态
        State = BattleState.None;
        inputState = BattleInputState.Normal;
        pendingItem = null;

        // 重置缓存值
        lastPlayerHP = 0;
        lastPlayerMana = 0;
        lastEnemyHP = 0;
        lastEnemyMana = 0;

        // 设为不活动，等待下次进入 Combat/Boss/Guide 房间再由房间脚本激活
        gameObject.SetActive(false);
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
            var spiritData = spiritQueue[i];
            spiritRuntimeData[spiritData] = new SpiritRuntimeData
            {
                CurrentHP = spiritData.MaxHP,
                MaxHP = spiritData.MaxHP,
                CurrentMP = spiritData.MaxMana,
                MaxMP = spiritData.MaxMana,
            };

            // 如果是死亡的Spirit，恢复为存活
            if (spiritAliveStatus != null && spiritAliveStatus.ContainsKey(spiritData))
            {
                spiritAliveStatus[spiritData] = true;
            }
        }

        Debug.Log("BattleController: All spirits reset to full HP/MP");
    }

    public Spirit Player => player;
    public Enemy Enemy => enemy;

    /// <summary>
    /// 清理战斗房间中的掉落展示（进入路线选择前调用）
    /// </summary>
    public void CleanupCombatDropVisual()
    {
        if (combatRoom != null)
        {
            combatRoom.CleanupDropVisual();
        }
    }

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
        if (spiritAliveStatus == null || index < 0 || index >= spiritQueue.Count)
            return false;

        var spiritData = spiritQueue[index];
        if (!spiritAliveStatus.ContainsKey(spiritData))
            return false;

        return spiritAliveStatus[spiritData];
    }

    /// <summary>
    /// 取消指定索引Spirit的死亡标记（当HP恢复到非0时调用）
    /// </summary>
    public void ReviveSpirit(int index)
    {
        if (spiritAliveStatus == null || index < 0 || index >= spiritQueue.Count)
        {
            Debug.LogWarning(
                $"BattleController: Cannot revive spirit at index {index} - invalid index or status not initialized"
            );
            return;
        }

        var spiritData = spiritQueue[index];
        if (!spiritAliveStatus.ContainsKey(spiritData))
        {
            Debug.LogWarning(
                $"BattleController: Cannot revive spirit at index {index} - spirit not found in alive status"
            );
            return;
        }

        // 检查该Spirit的HP是否大于0
        if (spiritRuntimeData != null && spiritRuntimeData.ContainsKey(spiritData))
        {
            var runtimeData = spiritRuntimeData[spiritData];
            if (runtimeData.CurrentHP > 0)
            {
                spiritAliveStatus[spiritData] = true;
                Debug.Log(
                    $"BattleController: Spirit {index} ({spiritData.DisplayName}) revived with HP={runtimeData.CurrentHP}/{runtimeData.MaxHP}"
                );
            }
            else
            {
                Debug.LogWarning($"BattleController: Cannot revive spirit {index} ({spiritData.DisplayName}) - HP is still 0");
            }
        }
    }

    /// <summary>
    /// 检查当前是否为强制切换（因心兽死亡）
    /// </summary>
    public bool IsForcedSwitchDueToDefeat()
    {
        return isForcedSwitchDueToDefeat;
    }

    /// <summary>
    /// 重置强制切换标志
    /// </summary>
    public void ResetForcedSwitchFlag()
    {
        isForcedSwitchDueToDefeat = false;
        Debug.Log("BattleController: Reset forced switch flag.");
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
        if (spiritRuntimeData != null && index >= 0 && index < spiritQueue.Count)
        {
            var spiritData = spiritQueue[index];
            if (spiritRuntimeData.ContainsKey(spiritData))
            {
                return spiritRuntimeData[spiritData];
            }
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

    /// <summary>
    /// 获取精灵队列（用于RestRoom等外部访问）
    /// </summary>
    public List<SpiritData> GetSpiritQueue()
    {
        return spiritQueue;
    }

    /// <summary>
    /// 更新指定精灵的运行时数据（用于RestRoom治疗）
    /// </summary>
    public void UpdateSpiritRuntimeData(int index, int newHP, int newMP)
    {
        if (index < 0 || index >= spiritQueue.Count || spiritRuntimeData == null)
        {
            Debug.LogWarning($"BattleController: Cannot update runtime data for invalid index {index}");
            return;
        }

        var spiritData = spiritQueue[index];
        if (!spiritRuntimeData.ContainsKey(spiritData))
        {
            Debug.LogWarning($"BattleController: Spirit {spiritData.DisplayName} not found in runtime data");
            return;
        }

        var currentData = spiritRuntimeData[spiritData];
        spiritRuntimeData[spiritData] = new SpiritRuntimeData
        {
            CurrentHP = newHP,
            MaxHP = currentData.MaxHP,
            CurrentMP = newMP,
            MaxMP = currentData.MaxMP
        };

        Debug.Log($"BattleController: Updated {spiritData.DisplayName} runtime data: HP={newHP}/{currentData.MaxHP}, MP={newMP}/{currentData.MaxMP}");

        // 如果是当前上场精灵，同步更新player实例
        if (index == currentSpiritIndex && player != null)
        {
            player.SetRuntimeHPMP(newHP, newMP);
        }
    }

    /// <summary>
    /// 将指定精灵的运行时HP/MP恢复到满值，并标记为存活（用于引导房/休整奖励等）。
    /// </summary>
    public void RestoreSpiritsToFull(IEnumerable<SpiritData> spirits)
    {
        if (spirits == null)
        {
            return;
        }

        if (spiritRuntimeData == null)
        {
            spiritRuntimeData = new System.Collections.Generic.Dictionary<SpiritData, SpiritRuntimeData>();
        }
        if (spiritAliveStatus == null)
        {
            spiritAliveStatus = new System.Collections.Generic.Dictionary<SpiritData, bool>();
        }

        int restored = 0;
        foreach (var spiritData in spirits)
        {
            if (spiritData == null)
            {
                continue;
            }

            spiritRuntimeData[spiritData] = new SpiritRuntimeData
            {
                CurrentHP = spiritData.MaxHP,
                MaxHP = spiritData.MaxHP,
                CurrentMP = spiritData.MaxMana,
                MaxMP = spiritData.MaxMana,
            };
            spiritAliveStatus[spiritData] = true;
            restored++;
        }

        // 若当前上场精灵也在恢复列表中，顺便同步一次
        if (player != null && player.Data != null && spiritRuntimeData.ContainsKey(player.Data))
        {
            var runtime = spiritRuntimeData[player.Data];
            player.SetRuntimeHPMP(runtime.CurrentHP, runtime.CurrentMP);
        }

        Debug.Log($"BattleController: Restored {restored} spirit(s) to full HP/MP");
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

        // 初心者羁绊：首次技能释放不消耗MP且不进入冷却（需要允许在MP不足时也能释放）
        bool hasBeginnerFreeSkill = false;
        BeginnerBuff beginnerBuff = null;
        if (model != null)
        {
            var buffs = model.GetBuffsForUnit(player);
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i] is BeginnerBuff b && b.CanTriggerFreeSkill())
                {
                    beginnerBuff = b;
                    hasBeginnerFreeSkill = true;
                    break;
                }
            }
        }

        if (player.Mana < skill.ManaCost && !hasBeginnerFreeSkill)
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
                        Debug.LogError(
                            $"BattleController: {(caster == player ? "Player" : "Enemy")} Skill Animator has no AnimatorController! Please create a basic AnimatorController with a 'Skill' state and assign it."
                        );
                        targetAnimator.gameObject.SetActive(false);
                        yield return new WaitForSeconds(skill.SkillAnimation.length);
                        yield break;
                    }

                    overrideController = new AnimatorOverrideController(
                        targetAnimator.runtimeAnimatorController
                    );
                    targetAnimator.runtimeAnimatorController = overrideController;
                }

                // 替换所有动画为当前技能的动画
                var overrides =
                    new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<
                        AnimationClip,
                        AnimationClip
                    >>();
                overrideController.GetOverrides(overrides);

                for (int i = 0; i < overrides.Count; i++)
                {
                    overrides[i] = new System.Collections.Generic.KeyValuePair<
                        AnimationClip,
                        AnimationClip
                    >(overrides[i].Key, skill.SkillAnimation);
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

        // 初心者：记录是否触发首次免费技能（用于：释放后返还MP + 不进入冷却 + 标记已触发）
        bool beginnerFreeSkillTriggered = false;
        BeginnerBuff beginnerBuff = null;
        int casterManaBefore = 0;
        if (isPlayerSkill && model != null && caster != null)
        {
            var buffs = model.GetBuffsForUnit(caster);
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i] is BeginnerBuff b && b.CanTriggerFreeSkill())
                {
                    beginnerBuff = b;
                    beginnerFreeSkillTriggered = true;
                    casterManaBefore = caster.Mana;
                    break;
                }
            }
        }

        skill.Execute(caster, target);
        Debug.Log($"BattleController: Target HP after skill: {target.HP}");

        // 刷新UI显示血条变化
        if (battleView != null)
            battleView.Refresh();

        // 初心者：返还MP并标记已使用首次技能
        if (beginnerFreeSkillTriggered && beginnerBuff != null && caster != null)
        {
            int spent = casterManaBefore - caster.Mana;
            if (spent > 0)
            {
                caster.RestoreMana(spent);
            }
            beginnerBuff.MarkFirstSkillUsed();
        }

        // 记录技能使用次数和冷却（玩家和敌人都需要）
        if (skillIndex >= 0 && model != null)
        {
            // 初心者：首次免费技能不计入使用次数
            if (!beginnerFreeSkillTriggered)
            {
                model.IncrementSkillUsage(skillIndex);
            }

            // 设置冷却
            if (skill.CooldownTurns > 0)
            {
                bool skipCooldown = false;

                // 初心者：首次技能不进入冷却
                if (beginnerFreeSkillTriggered)
                {
                    skipCooldown = true;
                }

                // 派对狂欢：释放技能时概率不进入冷却
                if (!skipCooldown && isPlayerSkill && caster != null)
                {
                    var buffs = model.GetBuffsForUnit(caster);
                    for (int i = 0; i < buffs.Count; i++)
                    {
                        if (buffs[i] is PartyTimeBuff partyTimeBuff && partyTimeBuff.TryTriggerNoCooldown())
                        {
                            skipCooldown = true;
                            break;
                        }
                    }
                }

                if (!skipCooldown)
                {
                    model.SetSkillCooldown(skillIndex, skill.CooldownTurns);
                    Debug.Log(
                        $"BattleController: {(isPlayerSkill ? "Player" : "Enemy")} skill {skillIndex} set on cooldown for {skill.CooldownTurns} turns"
                    );
                }
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

        // 不在这里调用 OnTurnEnd()，而是在敌人回合结束时调用
        // 这样确保 debuff 在敌人行动后才减少持续时间

        // 增加回合计数（模型负责）
        model?.IncrementTurn();

        // TurnStart debuff/buff 可能会在这里把玩家/敌人打死，需要立刻结算胜负/切换
        UpdateBattleStateAfterAction();
        if (State == BattleState.Victory || State == BattleState.Defeat)
        {
            if (battleView != null)
                battleView.Refresh();
            return;
        }

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

        // 检查敌人是否被控制（冰冻、睡眠等）
        if (model != null && model.IsUnitControlled(enemy))
        {
            string controlEffect = model.GetControlEffectName(enemy);
            Debug.Log($"BattleController: {enemy.DisplayName} 被 {controlEffect} 控制，无法行动！");

            // 敌人回合结束，处理Buff效果并减少持续时间
            model?.OnTurnEnd();

            // **关键修复**：检查战斗状态（buff效果可能导致敌人死亡）
            UpdateBattleStateAfterAction();

            // 如果战斗已经结束，不要切换回玩家回合
            if (State == BattleState.Victory || State == BattleState.Defeat)
            {
                Debug.Log($"BattleController: 战斗在敌人被控制期间结束，状态={State}");
                yield break;
            }

            // 敌人被控制，跳过行动，直接返回玩家回合
            State = BattleState.PlayerTurn;
            if (battleView != null)
                battleView.Refresh();
            yield break;
        }

        // AI决定使用的技能（传入BattleModel以检查冷却和使用次数）
        int skillIndex;
        var skill = enemyAI.DecideSkill(enemy, player, model, out skillIndex);
        if (skill == null)
        {
            Debug.Log("AIController: Enemy has no available skills");

            // 即使敌人没有可用技能，也需要触发回合结束的buff结算/过期，并立刻检查胜负
            model?.OnTurnEnd();
            UpdateBattleStateAfterAction();
            if (State == BattleState.Victory || State == BattleState.Defeat)
            {
                Debug.Log($"BattleController: Battle ended after enemy skip: {State}");
                yield break;
            }

            State = BattleState.PlayerTurn;
            yield break;
        }

        // 使用偏移后的索引来存储敌人技能的冷却和使用次数
        int enemySkillIndex = AIController.GetEnemySkillIndex(skillIndex);

        // 使用协程执行技能（包含动画）
        yield return StartCoroutine(
            ExecuteSkillWithAnimation(skill, enemySkillIndex, enemy, player, false)
        );

        // 敌人回合结束，处理Buff效果并减少持续时间
        model?.OnTurnEnd();

        // **关键修复**：检查战斗状态（可能在敌人行动时触发反伤导致敌人死亡）
        UpdateBattleStateAfterAction();

        // 如果战斗已经结束（Victory/Defeat），不要切换回玩家回合
        if (State == BattleState.Victory || State == BattleState.Defeat)
        {
            Debug.Log($"BattleController: 战斗在敌人回合后结束，状态={State}");
            yield break;
        }

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
                        bool success = false;
                        SpiritData capturedSpirit = null;

                        if (combatRoom != null)
                        {
                            (success, capturedSpirit) = combatRoom.AttemptCapture(earlyCapture: true);
                        }
                        else if (bossRoom != null)
                        {
                            (success, capturedSpirit) = bossRoom.AttemptCapture(earlyCapture: true);
                        }

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

            // 先尝试捕捉精灵（在移除数据之前）
            if (combatRoom != null)
            {
                // 先标记房间已清理，这样 AttemptCapture 中的 IsCleared 检查才能通过
                combatRoom.MarkAsCleared();

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

                // 捕捉完成后，从对象池中移除已击败的敌人和精灵数据
                combatRoom.RemoveCurrentEnemyFromPool();

                // 不再自动切换房间，由玩家点击"继续"按钮后触发
                Debug.Log("BattleController: 战斗胜利，等待玩家点击继续按钮离开房间");
            }
            else if (bossRoom != null)
            {
                // Boss房间：标记已清理
                bossRoom.MarkAsCleared();

                // 获取Boss名称
                string bossName = enemy?.DisplayName ?? "";
                Debug.Log($"[BattleController] ===== Boss被击败！=====");
                Debug.Log($"[BattleController] Boss DisplayName: '{bossName}' (长度: {bossName.Length})");

                // 详细打印每个字符（用于检测隐藏字符、全角半角等问题）
                if (!string.IsNullOrEmpty(bossName))
                {
                    string charDebug = "字符分析: ";
                    foreach (char c in bossName)
                    {
                        charDebug += $"[{c}({(int)c})] ";
                    }
                    Debug.Log($"[BattleController] {charDebug}");
                }

                // 尝试显示特殊Boss CG
                bool usedSpecialCG = false;
                if (battleView != null)
                {
                    Debug.Log($"[BattleController] 调用 battleView.ShowSpecialBossCG...");
                    usedSpecialCG = battleView.ShowSpecialBossCG(bossName, () =>
                    {
                        // CG播放完成后的回调：显示捕捉结果
                        Debug.Log("[BattleController] CG播放完成回调被触发");
                        HandleBossCaptureAfterCG(bossRoom);
                    });
                    Debug.Log($"[BattleController] ShowSpecialBossCG 返回值: {usedSpecialCG}");
                }
                else
                {
                    Debug.LogWarning("[BattleController] ⚠️ battleView 为 null！无法显示CG");
                }

                // 如果没有使用特殊CG，使用普通流程
                if (!usedSpecialCG)
                {
                    Debug.Log("[BattleController] 使用普通敌人死亡流程（没有特殊CG）");

                    // 显示普通的敌人死亡面板
                    if (battleView != null)
                    {
                        battleView.ShowEnemyDeathPanel();
                    }

                    // 直接执行捕捉逻辑
                    HandleBossCaptureAfterCG(bossRoom);
                }

                Debug.Log("BattleController: Boss战斗胜利，等待玩家点击继续按钮离开房间");
            }
            else
            {
                Debug.LogWarning(
                    "BattleController: combatRoom and bossRoom references are both null"
                );
            }

            // 标记敌人为已击败（作为备用机制）
            if (enemyData != null)
            {
                EnemyPool.MarkEnemyAsDefeated(enemyData);
            }

            return;
        }

        if (player != null && player.IsDead)
        {
            // 标记当前Spirit为死亡
            if (spiritAliveStatus != null && currentSpiritIndex >= 0 && currentSpiritIndex < spiritQueue.Count)
            {
                var currentSpiritData = spiritQueue[currentSpiritIndex];
                spiritAliveStatus[currentSpiritData] = false;
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
                // 标记为强制切换（因心兽死亡）
                isForcedSwitchDueToDefeat = true;

                // 自动切换到下一个存活的Spirit
                Debug.Log("BattleController: Current spirit defeated. Auto-switching to next alive spirit.");

                bool switchSuccess = TrySwitchToNextAliveSpirit();

                if (switchSuccess)
                {
                    Debug.Log("BattleController: Successfully auto-switched to next alive spirit.");

                    // 刷新UI显示
                    if (battleView != null)
                    {
                        battleView.Refresh();
                    }

                    // 重置强制切换标志（因为已经完成切换）
                    ResetForcedSwitchFlag();
                }
                else
                {
                    // 自动切换失败，回退到手动选择
                    Debug.LogWarning("BattleController: Auto-switch failed. Showing spirit switcher panel.");
                    if (battleView != null)
                    {
                        battleView.ShowSpiritSwitcherPanel();
                        battleView.Refresh();
                    }
                }
            }
            else
            {
                // 所有Spirit都死亡，战斗失败
                State = BattleState.Defeat;
                Debug.Log("BattleController: All spirits defeated. Battle lost.");

                // 显示战斗失败面板
                if (battleView != null)
                {
                    battleView.ShowLosePanel();
                }

                // 设置游戏状态为GameOver（但不显示额外的GameOverPanel）
                if (GameManagerService.Instance != null)
                {
                    GameManagerService.Instance.SetGameState(GameState.GameOver);
                    Debug.Log("BattleController: Game state set to GameOver");
                }
            }
        }
    }

    /// <summary>
    /// Boss CG播放完成后处理捕捉逻辑
    /// </summary>
    /// <param name="bossRoom">Boss房间引用</param>
    private void HandleBossCaptureAfterCG(BossRoom_cza bossRoom)
    {
        if (bossRoom == null)
        {
            Debug.LogWarning("[BattleController] HandleBossCaptureAfterCG: bossRoom为null");
            return;
        }

        // 尝试捕捉Boss对应的精灵
        var (success, capturedSpirit) = bossRoom.AttemptCapture();
        if (battleView != null)
        {
            if (success && capturedSpirit != null)
            {
                battleView.ShowCaptureSuccess(capturedSpirit.DisplayName);
                Debug.Log($"[BattleController] Boss捕捉成功: {capturedSpirit.DisplayName}");
            }
            else
            {
                battleView.ShowCaptureFailed();
                Debug.Log("[BattleController] Boss捕捉失败");
            }

            // 显示普通的敌人死亡面板（显示继续按钮）
            battleView.ShowEnemyDeathPanel();
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
    /// 从第一个槽位开始查找第一个存活的心兽（排除当前已死亡的）
    /// </summary>
    /// <returns>是否切换成功</returns>
    private bool TrySwitchToNextAliveSpirit()
    {
        // 从第一个槽位开始查找存活的Spirit
        for (int i = 0; i < spiritQueue.Count; i++)
        {
            // 跳过当前已死的心兽
            if (i == currentSpiritIndex)
                continue;

            if (IsSpiritAlive(i))
            {
                Debug.Log($"BattleController: Found alive spirit at index {i}, switching...");
                return PerformSpiritSwitch(i);
            }
        }

        // 没有找到存活的Spirit
        Debug.LogWarning("BattleController: No alive spirit found for auto-switch");
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
        if (player != null && currentSpiritIndex >= 0 && currentSpiritIndex < spiritQueue.Count)
        {
            var currentSpiritData = spiritQueue[currentSpiritIndex];
            spiritRuntimeData[currentSpiritData] = new SpiritRuntimeData
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

        // 使用BattleModel中保存的技能列表创建Spirit（如果存在）
        SpiritBattleState savedState = model.GetSpiritState(nextSpiritData);
        if (savedState != null && savedState.SelectedSkills.Count > 0)
        {
            player = new Spirit(nextSpiritData, savedState.SelectedSkills);
        }
        else
        {
            player = new Spirit(nextSpiritData); // 首次出场，随机选择技能
        }

        // 恢复目标Spirit的运行时数据
        if (spiritRuntimeData.ContainsKey(nextSpiritData))
        {
            var runtimeData = spiritRuntimeData[nextSpiritData];
            // 直接恢复之前记录的HP/MP（不走伤害减伤逻辑）
            player.SetRuntimeHPMP(runtimeData.CurrentHP, runtimeData.CurrentMP);
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
                if (spiritRuntimeData != null && currentSpiritIndex >= 0 && currentSpiritIndex < spiritQueue.Count)
                {
                    var currentSpiritData = spiritQueue[currentSpiritIndex];
                    spiritRuntimeData[currentSpiritData] = new SpiritRuntimeData
                    {
                        CurrentHP = player.HP,
                        MaxHP = player.MaxHP,
                        CurrentMP = player.Mana,
                        MaxMP = player.MaxMana,
                    };

                    // 检查当前精灵是否从死亡状态恢复
                    if (
                        player.HP > 0
                        && spiritAliveStatus != null
                        && spiritAliveStatus.ContainsKey(currentSpiritData)
                        && !spiritAliveStatus[currentSpiritData]
                    )
                    {
                        ReviveSpirit(currentSpiritIndex);
                    }
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
        Debug.Log(
            $"[BattleController] OnItemUseRequested enter, item={(item != null ? item.DisplayName : "NULL")}, state={State}, inputState={inputState}, battleViewBound={(battleView != null)}"
        );
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

        Debug.Log(
            $"BattleController: Item use requested: {item.DisplayName}, TargetMode={item.TargetMode}"
        );

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
                    battleView.ShowItemTargetSelector();
                    Debug.Log("BattleController: Waiting for player to select a Spirit as target");
                }
                else
                {
                    Debug.LogError(
                        "BattleController: UI_BattleView not assigned! Please assign it in Inspector."
                    );
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
            Debug.LogWarning(
                "BattleController: OnSpiritSelectedAsItemTarget called but not waiting for target"
            );
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

        Debug.Log(
            $"BattleController: Spirit {spiritIndex} selected as item target: {target.DisplayName}"
        );

        // 使用道具
        UseItemOnTarget(pendingItem, target, spiritIndex);

        // 重置状态
        inputState = BattleInputState.Normal;
        pendingItem = null;

        // 关闭Spirit切换器
        if (battleView != null)
        {
            battleView.HideItemTargetSelector();
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
                battleView.HideItemTargetSelector();
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

        Debug.Log(
            $"BattleController: Using item {item.DisplayName} on {(target != null ? target.DisplayName : "null (群体)")}"
        );
        Debug.Log(
            $"BattleController: Target HP before use: {(target != null ? target.HP + "/" + target.MaxHP : "N/A")}"
        );

        // 通过InventoryManager使用道具（直接传递ItemData）
        if (InventoryManager.Instance != null)
        {
            Debug.Log(
                $"BattleController: Calling InventoryManager.UseItem with ItemData={item.DisplayName}"
            );
            InventoryManager.Instance.UseItem(item, user, target);
        }
        else
        {
            // 如果没有InventoryManager，直接调用道具的Use方法
            Debug.Log($"BattleController: InventoryManager not found, calling item.Use directly");
            item.Use(user, target);
        }

        Debug.Log(
            $"BattleController: Target HP after use: {(target != null ? target.HP + "/" + target.MaxHP : "N/A")}"
        );

        // 如果目标是非当前Spirit，保存使用后的数据
        // 注意：targetSpiritIndex 是 OwnedSpirits 的索引，需要转换为 spiritQueue 的索引
        if (targetSpiritIndex >= 0 && target is Spirit targetSpirit)
        {
            // 获取 OwnedSpirits 中对应的 SpiritData
            var ownedSpirits = PlayerManager.Instance != null
                ? PlayerManager.Instance.GetOwnedSpirits()
                : (playerData != null ? playerData.GetOwnedSpirits() : null);

            if (ownedSpirits != null && targetSpiritIndex < ownedSpirits.Count)
            {
                var targetSpiritData = ownedSpirits[targetSpiritIndex];
                // 查找在 spiritQueue 中的索引
                int queueIndex = spiritQueue != null ? spiritQueue.IndexOf(targetSpiritData) : -1;

                if (queueIndex >= 0 && queueIndex != currentSpiritIndex)
                {
                    Debug.Log($"BattleController: Saving runtime data for Spirit at queue index {queueIndex} (owned index {targetSpiritIndex})");
                    SaveSpiritRuntimeDataAfterItem(queueIndex, targetSpirit);
                }
                else
                {
                    Debug.Log($"BattleController: Spirit not in queue or is current, queueIndex={queueIndex}, currentSpiritIndex={currentSpiritIndex}");
                }
            }
        }

        // 刷新UI
        if (battleView != null)
            battleView.Refresh();

        // 处理进化/替换类道具：在使用后替换玩家拥有/部署列表中的目标精灵
        TryApplyEvolutionReplacement(item, targetSpiritIndex);

        Debug.Log($"BattleController: Item {item.DisplayName} used successfully");

        // 立即刷新背包UI，确保物品使用后slot立即更新
        var inventoryView = FindObjectOfType<UI_InventoryView>();
        if (inventoryView != null)
        {
            Debug.Log("[BattleController] 手动触发 InventoryView 刷新");
            inventoryView.UpdateInventoryUI();
        }
        else
        {
            Debug.LogWarning("[BattleController] 未找到 UI_InventoryView，无法手动刷新背包UI");
        }
    }

    private void TryApplyEvolutionReplacement(ItemData item, int targetSpiritIndex)
    {
        if (item == null || targetSpiritIndex < 0)
            return;

        var effects = item.Effects;
        if (effects == null)
            return;

        Evolution evo = null;
        foreach (var eff in effects)
        {
            if (eff is Evolution e)
            {
                evo = e;
                break;
            }
        }

        if (evo == null || evo.TargetSpirit == null)
            return;

        var newSpiritData = evo.TargetSpirit;

        // 更新PlayerManager拥有列表
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ReplaceOwnedSpirit(targetSpiritIndex, newSpiritData);
        }

        // 更新战斗内的部署列表/运行时数据
        if (spiritQueue != null && targetSpiritIndex < spiritQueue.Count)
        {
            var oldSpiritData = spiritQueue[targetSpiritIndex];
            spiritQueue[targetSpiritIndex] = newSpiritData;

            // 从旧精灵数据移除运行时数据，添加新精灵数据
            if (spiritRuntimeData != null)
            {
                if (spiritRuntimeData.ContainsKey(oldSpiritData))
                {
                    spiritRuntimeData.Remove(oldSpiritData);
                }

                spiritRuntimeData[newSpiritData] = new SpiritRuntimeData
                {
                    CurrentHP = newSpiritData.MaxHP,
                    MaxHP = newSpiritData.MaxHP,
                    CurrentMP = newSpiritData.MaxMana,
                    MaxMP = newSpiritData.MaxMana,
                };
            }

            if (spiritAliveStatus != null)
            {
                if (spiritAliveStatus.ContainsKey(oldSpiritData))
                {
                    spiritAliveStatus.Remove(oldSpiritData);
                }

                spiritAliveStatus[newSpiritData] = true;
            }

            // 如果是当前上场精灵，立即切换实体
            if (targetSpiritIndex == currentSpiritIndex)
            {
                player = new Spirit(newSpiritData);
                model.UpdatePlayer(player);
            }
        }

        // 刷新UI槽位显示
        if (battleView != null)
        {
            battleView.RefreshSpiritSlots();
            battleView.RefreshItemUseSlots(true); // 重新加载精灵数据
            battleView.Refresh();
        }

        Debug.Log($"BattleController: Evolution replacement applied at index {targetSpiritIndex} -> {newSpiritData.DisplayName}");
    }

    /// <summary>
    /// 根据索引获取Spirit作为目标（支持从OwnedSpirits列表选择）
    /// </summary>
    private IBattleUnit GetSpiritAsTarget(int spiritIndex)
    {
        // 获取所有拥有的精灵（与 ItemUseSlot 显示的一致）
        var ownedSpirits = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetOwnedSpirits()
            : (playerData != null ? playerData.GetOwnedSpirits() : null);

        if (ownedSpirits == null || spiritIndex < 0 || spiritIndex >= ownedSpirits.Count)
        {
            Debug.LogWarning($"BattleController: Invalid spiritIndex {spiritIndex}, ownedSpirits count={(ownedSpirits?.Count ?? 0)}");
            return null;
        }

        var spiritData = ownedSpirits[spiritIndex];
        if (spiritData == null)
        {
            Debug.LogWarning($"BattleController: SpiritData at index {spiritIndex} is null");
            return null;
        }

        // 检查是否是当前上场的精灵
        if (player != null && player.Data == spiritData)
        {
            Debug.Log($"BattleController: Target is current player Spirit: {player.DisplayName}");
            return player;
        }

        // 检查是否在spiritQueue中（已部署的精灵）
        int queueIndex = spiritQueue != null ? spiritQueue.IndexOf(spiritData) : -1;

        // 创建临时Spirit实例作为目标
        SpiritBattleState savedState = model.GetSpiritState(spiritData);
        Spirit tempSpirit;
        if (savedState != null && savedState.SelectedSkills.Count > 0)
        {
            tempSpirit = new Spirit(spiritData, savedState.SelectedSkills);
        }
        else
        {
            tempSpirit = new Spirit(spiritData);
        }

        // 如果在spiritQueue中，恢复运行时数据
        if (queueIndex >= 0 && spiritRuntimeData != null && spiritRuntimeData.ContainsKey(spiritData))
        {
            var runtimeData = spiritRuntimeData[spiritData];
            tempSpirit.SetRuntimeHPMP(runtimeData.CurrentHP, runtimeData.CurrentMP);
        }

        Debug.Log(
            $"BattleController: Created temp Spirit for index {spiritIndex} ({spiritData.DisplayName}): HP={tempSpirit.HP}/{tempSpirit.MaxHP}, MP={tempSpirit.Mana}/{tempSpirit.MaxMana}"
        );

        return tempSpirit;
    }

    /// <summary>
    /// 使用道具后，保存Spirit的运行时数据
    /// </summary>
    private void SaveSpiritRuntimeDataAfterItem(int spiritIndex, Spirit spirit)
    {
        if (spirit == null || spiritRuntimeData == null)
            return;

        if (spiritIndex < 0 || spiritIndex >= spiritQueue.Count)
            return;

        var spiritData = spiritQueue[spiritIndex];
        spiritRuntimeData[spiritData] = new SpiritRuntimeData
        {
            CurrentHP = spirit.HP,
            MaxHP = spirit.MaxHP,
            CurrentMP = spirit.Mana,
            MaxMP = spirit.MaxMana,
        };

        Debug.Log(
            $"BattleController: Saved Spirit {spiritIndex} runtime data after item use: HP={spirit.HP}/{spirit.MaxHP}, MP={spirit.Mana}/{spirit.MaxMana}"
        );

        // 如果该精灵的HP恢复到大于0，取消死亡标记
        if (
            spirit.HP > 0
            && spiritAliveStatus != null
            && spiritAliveStatus.ContainsKey(spiritData)
            && !spiritAliveStatus[spiritData]
        )
        {
            ReviveSpirit(spiritIndex);
        }
    }

    #endregion
}
