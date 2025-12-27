using UnityEngine;

namespace DreamWeavers.Rooms
{
    /// <summary>
    /// Boss房间：进入房间后使用Inspector配置的Boss敌人开始战斗
    /// 继承 RoomBase_cza，以保持与现有房间系统一致
    /// 与CombatRoom_cza结构一致，但敌人由Inspector直接指定而非对象池随机
    /// 支持4个楼层不同的Boss配置
    /// </summary>
    public class BossRoom_cza : RoomBase_cza
    {
        [Header("Boss敌人配置（按楼层）")]
        [SerializeField] private EnemyData floor1BossData; // 第1层Boss敌人数据
        [SerializeField] private EnemyData floor2BossData; // 第2层Boss敌人数据
        [SerializeField] private EnemyData floor3BossData; // 第3层Boss敌人数据
        [SerializeField] private EnemyData floor4BossData; // 第4层Boss敌人数据
        
        [Header("Boss对应精灵配置（按楼层，用于捕捉）")]
        [SerializeField] private SpiritData floor1SpiritData; // 第1层Boss对应的精灵
        [SerializeField] private SpiritData floor2SpiritData; // 第2层Boss对应的精灵
        [SerializeField] private SpiritData floor3SpiritData; // 第3层Boss对应的精灵
        [SerializeField] private SpiritData floor4SpiritData; // 第4层Boss对应的精灵
        
        [Header("玩家数据")]
        [SerializeField] private PlayerData playerData; // 当前玩家数据（用于传递到战斗）

        [Header("掉落设置")]
        [SerializeField] private ItemPool itemPool; // 击败Boss后从此池随机掉落一个道具（可选）
        [SerializeField] private Transform spawnPoint; // 掉落展示的生成位置（仅用于道具展示）
        [Min(1)] [SerializeField] private int dropQuantity = 1; // 掉落数量
        [SerializeField] private GameObject pickupPrefab; // 掉落展示用预制体（可选）
        [SerializeField] private bool useWeightedRandom = false; // 是否使用权重随机掉落

        [Header("战斗UI引用")]
        [SerializeField] private BattleController battleController; // 战斗控制器引用
        [SerializeField] private UI_BattleView battleView; // 战斗UI视图引用

        // 运行时状态
        private bool spawned = false;
        private Enemy enemyModel; // 敌人模型（非 MonoBehaviour），来自 EnemyModel
        private SpiritData selectedSpirit; // 与当前敌人对应的精灵数据
        private EnemyData selectedEnemy; // 当前选中的敌人数据（来自Inspector配置）
        private bool itemDropped; // 是否已触发掉落，避免重复
        private bool roomCleared; // 外部标记房间已清理（由 BattleController 调用）
        private bool spiritCaptured; // 是否已捕捉精灵，避免重复捕捉

        // private void OnEnable()
        // {
        //     Debug.Log("[BossRoom] OnEnable: 激活Boss房间");
            
        //     // 自动查找 BattleController 和 BattleView（如果未在 Inspector 中配置）
        //     if (battleController == null)
        //     {
        //         battleController = FindObjectOfType<BattleController>(true);
        //     }
        //     if (battleView == null)
        //     {
        //         battleView = FindObjectOfType<UI_BattleView>(true);
        //     }
            
        //     // 注意：不在 OnEnable 中激活战斗，因为 Panel 显隐会频繁触发 OnEnable/OnDisable
        //     // 战斗的激活/停止由 EnterRoom/ExitRoom 控制
        //     Debug.Log("[BossRoom] OnEnable: Boss房间组件启用（等待 EnterRoom 开始战斗）");
        // }

        private void OnDisable()
        {
            // Panel 被隐藏时会触发 OnDisable，不做任何处理
            // 实际清理由 ExitRoom 或战斗结束逻辑处理
        }

        public override void EnterRoom()
        {
          
            
            // 每次进入房间都重新准备战斗（重置spawned标志）
            ResetRoom();
            
            // 1. 选择敌人（直接使用Inspector配置的enemyData）
            SelectEnemy();
            spawned = true;

            // 2. 触发进入房间对话（基于敌人DisplayName）
            TriggerRoomEnterDialogue();

            // 3. 激活 BattleController 并传入数据
            StartBattle();
        }

        /// <summary>
        /// 重置房间状态，准备新的战斗
        /// </summary>
        public void ResetRoom()
        {
            Debug.Log($"[BossRoom] ResetRoom: 重置房间状态 (当前enemyModel={(enemyModel != null ? $"HP={enemyModel.HP}" : "null")})");
            spawned = false;
            itemDropped = false;
            roomCleared = false;
            spiritCaptured = false;
            enemyModel = null;
            selectedEnemy = null;
            selectedSpirit = null;
        }

        /// <summary>
        /// 选择敌人（根据当前楼层选择对应的Boss）
        /// </summary>
        private void SelectEnemy()
        {
            Debug.Log("[BossRoom] SelectEnemy 开始...");
            selectedEnemy = null;
            selectedSpirit = null;

            // 获取当前楼层
            int currentFloor = 1;
            var sm = RoomStateMachine_cza.Instance;
            if (sm != null && sm.CurrentMap != null)
            {
                currentFloor = sm.CurrentMap.FloorIndex;
            }
            Debug.Log($"[BossRoom] 当前楼层: {currentFloor}");

            // 根据楼层选择对应的Boss数据
            EnemyData bossData = GetBossDataForFloor(currentFloor);
            SpiritData bossSpiritData = GetSpiritDataForFloor(currentFloor);

            if (bossData != null)
            {
                Debug.Log($"[BossRoom] 使用第{currentFloor}层Boss: {bossData.name}");
                selectedEnemy = bossData;
                selectedSpirit = bossSpiritData;
            }
            else
            {
                Debug.LogError($"[BossRoom] 第{currentFloor}层的 BossData 未配置！请在 Inspector 中配置 floor{currentFloor}BossData");
                return;
            }

            // 初始化敌人模型（用于HP/Mana判定）
            if (selectedEnemy != null)
            {
                enemyModel = new Enemy(selectedEnemy);
                Debug.Log($"[BossRoom] Boss模型已创建: Name={selectedEnemy.name}, HP={enemyModel.HP}, Mana={enemyModel.Mana}");
            }
            else
            {
                Debug.LogError("[BossRoom] 未能获取敌人数据！请检查 EnemyData 配置");
            }
        }

        /// <summary>
        /// 根据楼层获取对应的Boss敌人数据
        /// </summary>
        private EnemyData GetBossDataForFloor(int floor)
        {
            switch (floor)
            {
                case 1: return floor1BossData;
                case 2: return floor2BossData;
                case 3: return floor3BossData;
                case 4: return floor4BossData;
                default:
                    Debug.LogWarning($"[BossRoom] 未配置第{floor}层Boss，使用第1层Boss作为默认");
                    return floor1BossData;
            }
        }

        /// <summary>
        /// 根据楼层获取对应的Boss精灵数据
        /// </summary>
        private SpiritData GetSpiritDataForFloor(int floor)
        {
            switch (floor)
            {
                case 1: return floor1SpiritData;
                case 2: return floor2SpiritData;
                case 3: return floor3SpiritData;
                case 4: return floor4SpiritData;
                default:
                    Debug.LogWarning($"[BossRoom] 未配置第{floor}层Boss精灵，使用第1层作为默认");
                    return floor1SpiritData;
            }
        }

        /// <summary>
        /// 激活 BattleController 并开始战斗
        /// </summary>
        private void StartBattle()
        {
            Debug.Log("[BossRoom] StartBattle 开始...");
            
            // 使用已缓存的引用，如果没有则查找
            var bc = battleController;
            if (bc == null)
            {
                bc = FindObjectOfType<BattleController>();
                if (bc == null)
                {
                    // 尝试查找未激活的
                    var all = Resources.FindObjectsOfTypeAll<BattleController>();
                    if (all != null && all.Length > 0)
                    {
                        bc = all[0];
                        Debug.Log($"[BossRoom] 找到未激活的 BattleController: {bc.gameObject.name}");
                    }
                }
                battleController = bc; // 缓存引用
            }
            
            if (bc == null)
            {
                Debug.LogError("[BossRoom] 未找到 BattleController！请确保场景中存在 BattleController");
                return;
            }
            
            Debug.Log($"[BossRoom] 找到 BattleController: {bc.gameObject.name}, active={bc.gameObject.activeInHierarchy}");

            // 检查必要数据
            if (playerData == null)
            {
                Debug.LogError("[BossRoom] PlayerData 未配置！请在 Inspector 中配置 PlayerData");
                return;
            }
            if (selectedEnemy == null)
            {
                Debug.LogError("[BossRoom] EnemyData 未获取！请检查 Inspector 中的 EnemyData 配置");
                return;
            }

            // 确保 BattleController 激活（OnEnable 应该已经激活了，这里做双重保险）
            if (!bc.gameObject.activeInHierarchy)
            {
                bc.gameObject.SetActive(true);
                Debug.Log("[BossRoom] 激活 BattleController");
            }

            // 传入数据并开始战斗（同时传入自身引用）
            // 注意：不要在这里调用 ShowBattlePanel()，因为 BeginBattleWith -> InitializeBattle -> battleView.Bind() 会自动激活并绑定UI
            Debug.Log($"[BossRoom] 开始Boss战斗: Player={playerData.name}, Boss={selectedEnemy.name}");
            bc.BeginBattleWith(playerData, selectedEnemy, this);
        }

        public bool IsCleared()
        {
            // 房间清理判定：优先检查外部标记（由 BattleController 设置）
            if (roomCleared)
            {
                if (!itemDropped)
                {
                    TryDropRandomItem();
                    itemDropped = true;
                }
                return true;
            }
            
            // 次选：如果已生成且该敌人实例被销毁（死亡），则视为清理完成
            if (!spawned) return false;
            if (enemyModel != null)
            {
                bool defeated = enemyModel.HP <= 0 || enemyModel.Mana <= 0;
                if (defeated && !itemDropped)
                {
                    TryDropRandomItem();
                    itemDropped = true;
                }
                return defeated;
            }
            return false;
        }

        /// <summary>
        /// 外部标记房间已清理（由 BattleController 在敌人死亡时调用）
        /// </summary>
        public void MarkAsCleared()
        {
            roomCleared = true;
            Debug.Log("[BossRoom] Boss房间已被外部标记为清理完成");
        }

        // 提供敌人模型访问，便于其他系统修改其 HP/Mana
        public Enemy GetEnemyModel() => enemyModel;

        // 提供当前敌人/精灵数据访问
        public EnemyData GetSelectedEnemyData() => selectedEnemy;
        public SpiritData GetSelectedSpiritData() => selectedSpirit;

        /// <summary>
        /// 触发进入房间对话（基于敌人DisplayName）
        /// </summary>
        private void TriggerRoomEnterDialogue()
        {
            Debug.Log("[BossRoom] TriggerRoomEnterDialogue 开始...");
            
            // 检查敌人数据是否已选择
            if (selectedEnemy == null)
            {
                Debug.LogWarning("[BossRoom] 无法触发对话：敌人数据为空");
                return;
            }
            
            // 检查DialogControllerService是否可用
            var dialogService = DreamWeavers.Services.DialogControllerService.Instance;
            if (dialogService == null)
            {
                Debug.LogWarning("[BossRoom] 无法触发对话：DialogControllerService不可用");
                return;
            }
            
            // 获取当前房间类型（Boss房间）
            RoomType_cza roomType = RoomType_cza.Boss;
            
            // 触发基于敌人DisplayName的对话
            try
            {
                dialogService.StartDialogueForRoomWithEnemy(roomType, selectedEnemy);
                Debug.Log($"[BossRoom] 成功触发对话 - 房间类型: {roomType}, 敌人: {selectedEnemy.DisplayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossRoom] 触发对话时发生错误: {e.Message}");
            }
        }

        /// <summary>
        /// 尝试捕捉当前敌人对应的精灵
        /// </summary>
        /// <param name="earlyCapture">是否为提前捕捉（狩猎大师羁绊）</param>
        /// <returns>捕捉成功返回(true, SpiritData)，失败返回(false, null)</returns>
        public (bool success, SpiritData spirit) AttemptCapture(bool earlyCapture = false)
        {
            Debug.Log($"[BossRoom] AttemptCapture 开始: earlyCapture={earlyCapture}, IsCleared={IsCleared()}, spiritCaptured={spiritCaptured}, selectedSpirit={(selectedSpirit != null ? selectedSpirit.name : "null")}, playerData={(playerData != null ? playerData.name : "null")}");

            // 检查是否已经捕捉过，避免重复捕捉
            if (spiritCaptured)
            {
                Debug.Log("[BossRoom] 已经捕捉过精灵，跳过重复捕捉");
                return (false, null);
            }

            // 检查房间是否已清理（或是否为提前捕捉）
            if (!earlyCapture && !IsCleared())
            {
                Debug.Log("[BossRoom] 房间未清理完成，无法捕捉");
                return (false, null);
            }

            // 确定要捕捉的SpiritData
            SpiritData spiritToCapture = selectedSpirit;

            if (spiritToCapture == null)
            {
                Debug.LogWarning("[BossRoom] 无法确定要捕捉的SpiritData - spiritData 未在 Inspector 中配置");
                return (false, null);
            }

            Debug.Log($"[BossRoom] 准备捕捉精灵: {spiritToCapture.DisplayName}");

            // TODO: 添加捕捉概率系统（目前100%成功）
            // 可以在这里添加随机数判定或其他捕捉机制

            // 使用PlayerManager捕捉精灵（同时更新运行时Player和PlayerData）
            bool success = false;
            if (PlayerManager.Instance != null)
            {
                success = PlayerManager.Instance.CaptureSpirit(spiritToCapture);
                if (success)
                {
                    spiritCaptured = true;
                    string captureType = earlyCapture ? "提前捕捉成功（诱捕）" : "捕捉成功";
                    Debug.Log($"[BossRoom] {captureType}: {spiritToCapture.DisplayName}");
                    return (true, spiritToCapture);
                }
            }
            else
            {
                Debug.LogWarning("[BossRoom] PlayerManager.Instance为null，尝试直接修改PlayerData");

                // 降级方案：直接修改PlayerData（仅编辑器可见）
                if (playerData != null)
                {
                    var owned = playerData.GetOwnedSpirits();
                    int beforeCount = owned.Count;
                    owned.Add(spiritToCapture);
                    playerData.OwnedSpirits = owned.ToArray();

                    spiritCaptured = true;

                    Debug.Log($"[BossRoom] 捕捉成功（仅PlayerData）! 精灵={spiritToCapture.DisplayName}, 拥有精灵数: {beforeCount} -> {playerData.OwnedSpirits.Length}");

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                    string captureType = earlyCapture ? "提前捕捉成功（诱捕）" : "捕捉成功";
                    Debug.Log($"[BossRoom] {captureType}: {spiritToCapture.DisplayName}");
                    return (true, spiritToCapture);
                }
            }

            Debug.LogWarning("[BossRoom] PlayerManager和PlayerData都无法使用，捕捉失败");
            return (false, null);
        }

        public override void ExitRoom()
        {
            // 捕捉精灵：离开房间时，如果已清理敌人，则将对应SpiritData添加到玩家拥有列表
            if (playerData == null)
            {
                Debug.LogWarning("[BossRoom] PlayerData 未配置，无法捕捉精灵");
                return;
            }

            // 检查是否已经捕捉过，避免重复捕捉
            if (spiritCaptured)
            {
                Debug.Log("[BossRoom] ExitRoom: 已经捕捉过精灵，跳过重复捕捉");
                return;
            }

            // 仅在房间清理完成（敌人被击败或实例销毁）时执行捕捉
            if (!IsCleared())
            {
                Debug.Log("[BossRoom] 房间未清理完成，不执行捕捉精灵");
                return;
            }

            // 确定要添加的SpiritData：使用Inspector配置的spiritData
            SpiritData spiritToAdd = selectedSpirit;
            if (spiritToAdd == null)
            {
                Debug.LogWarning("[BossRoom] 无法确定要捕捉的SpiritData（spiritData 未在 Inspector 中配置）");
                return;
            }

            // 使用PlayerManager捕捉精灵（同时更新运行时Player和PlayerData）
            bool success = false;
            if (PlayerManager.Instance != null)
            {
                success = PlayerManager.Instance.CaptureSpirit(spiritToAdd);
                if (success)
                {
                    spiritCaptured = true;
                    Debug.Log($"[BossRoom] ExitRoom 捕捉精灵成功: {spiritToAdd.DisplayName}");
                }
            }
            else
            {
                Debug.LogWarning("[BossRoom] ExitRoom: PlayerManager.Instance为null，尝试直接修改PlayerData");

                // 降级方案：直接修改PlayerData（仅编辑器可见）
                if (playerData != null)
                {
                    var owned = playerData.GetOwnedSpirits();
                    owned.Add(spiritToAdd);
                    playerData.OwnedSpirits = owned.ToArray();

                    spiritCaptured = true;

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                    Debug.Log($"[BossRoom] ExitRoom 捕捉精灵成功（仅PlayerData）: {spiritToAdd.DisplayName}");
                }
            }
        }

        // 击败Boss时从对象池随机掉落一个道具，并写入背包与玩家数据
        private void TryDropRandomItem()
        {
            if (itemPool == null || itemPool.IsEmpty)
            {
                Debug.LogWarning("[BossRoom] itemPool 未配置或为空，无法掉落道具");
                return;
            }

            var item = useWeightedRandom ? itemPool.GetWeightedRandomItem() : itemPool.GetRandomItem();
            if (item == null)
            {
                Debug.LogWarning("[BossRoom] 未能从对象池抽到掉落道具");
                return;
            }

            // 1) 运行时背包：更新 UI
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                bool ok = inv.AddItem(item, Mathf.Max(1, dropQuantity));
                if (!ok)
                {
                    Debug.LogWarning("[BossRoom] 掉落添加到背包失败，可能背包已满或参数非法");
                }
            }
            else
            {
                Debug.LogWarning("[BossRoom] 未找到 InventoryManager.Instance，背包未更新");
            }

            // 2) 玩家数据：写入 InitialItems 以持久化当前拥有
            if (playerData != null)
            {
                var list = playerData.GetInitialItems();
                list.Add(new ItemStackConfig { Item = item, Count = Mathf.Max(1, dropQuantity) });
                playerData.InitialItems = list.ToArray();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                Debug.Log($"[BossRoom] 掉落道具：{item.DisplayName} x{dropQuantity} 已写入玩家数据与背包");
            }
            else
            {
                Debug.LogWarning("[BossRoom] PlayerData 未配置，掉落未写入玩家数据资产");
            }

            // 3) 可选：在房间中生成一个展示预制体
            if (pickupPrefab != null)
            {
                var sp = spawnPoint != null ? spawnPoint : transform;
                var pickup = Instantiate(pickupPrefab, sp.position, sp.rotation);
                var display = pickup.GetComponent<ItemDisplay>();
                if (display != null)
                {
                    display.item = item;
                    display.Refresh();
                }
            }
        }
    }
}
