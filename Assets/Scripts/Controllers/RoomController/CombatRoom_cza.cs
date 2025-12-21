using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 

namespace DreamWeavers.Rooms
{
    // 战斗房间（精简版）：进入房间后在固定点生成一个不移动的敌人
    // 继承 RoomBase_cza，以保持与现有房间系统一致
    public class CombatRoom_cza : RoomBase_cza
    {
        [Header("敌人选择")]
        [SerializeField] private EnemyData enemyData; // 备用：当对象池不可用时使用
        [SerializeField] private PlayerData playerData; // 当前玩家数据（用于传递到战斗）

        [Header("对象池与随机配置")]
        [SerializeField] private EnemyPool enemyPool; // 敌人对象池（含敌人与对应精灵）
        [SerializeField] private bool useWeightedRandom = false; // 是否使用权重随机

        [Header("掉落设置")]
        [SerializeField] private ItemPool itemPool; // 击败敌人后从此池随机掉落一个道具（可选）
        [SerializeField] private Transform spawnPoint; // 掉落展示的生成位置（仅用于道具展示）
        [Min(1)] [SerializeField] private int dropQuantity = 1; // 掉落数量
        [SerializeField] private GameObject pickupPrefab; // 掉落展示用预制体（可选）

        [Header("战斗UI引用")]
        [SerializeField] private BattleController battleController; // 战斗控制器引用
        [SerializeField] private UI_BattleView battleView; // 战斗UI视图引用

        // 运行时状态
        private bool spawned = false;
        private Enemy enemyModel; // 敌人模型（非 MonoBehaviour），来自 EnemyModel
        private SpiritData selectedSpirit; // 与当前敌人对应的精灵数据
        private EnemyData selectedEnemy; // 当前选中的敌人数据（来自对象池或手动配置）
        private bool itemDropped; // 是否已触发掉落，避免重复
        private bool roomCleared; // 外部标记房间已清理（由 BattleController 调用）
        private bool spiritCaptured; // 是否已捕捉精灵，避免重复捕捉
        private GameObject dropPickupInstance; // 战斗掉落展示实例
        private ItemData pendingDropItem;
        private int pendingDropQuantity;
        private bool pendingDropGranted;
        private bool subscribedRoomCompleted;
        private bool subscribedBranchChoices;
        private Coroutine waitRoomStateCo;

        private void OnEnable()
        {
            SubscribeRoomCompletedEvent();
        }

        private void OnDisable()
        {
            UnsubscribeRoomCompletedEvent();
            DestroyDropPickupInstance();
            // Panel 被隐藏时会触发 OnDisable，不做任何处理
            // 实际清理由 ExitRoom 或战斗结束逻辑处理
        }

        public override void EnterRoom()
        {
            Debug.Log("[CombatRoom] ========== EnterRoom 被调用 ==========");
            Debug.Log($"[CombatRoom] spawned={spawned}, enemyPool={(enemyPool != null ? "已配置" : "未配置")}, playerData={(playerData != null ? playerData.name : "null")}");
            
            // 每次进入房间都重新选择敌人（重置spawned标志）
            // 这样每次进入新的战斗房间都会从pool随机获取敌人
            ResetRoom();
            
            // 1. 从对象池随机选择敌人
            SelectEnemyFromPool();
            spawned = true;

            // 2. 激活 BattleController 并传入数据
            StartBattle();
        }

        /// <summary>
        /// 重置房间状态，准备新的战斗
        /// </summary>
        public void ResetRoom()
        {
            Debug.Log($"[CombatRoom] ResetRoom: 重置房间状态 (当前enemyModel={(enemyModel != null ? $"HP={enemyModel.HP}" : "null")})");
            spawned = false;
            itemDropped = false;
            roomCleared = false;
            spiritCaptured = false;
            enemyModel = null;
            selectedEnemy = null;
            selectedSpirit = null;
            DestroyDropPickupInstance();
            pendingDropItem = null;
            pendingDropQuantity = 0;
            pendingDropGranted = false;
        }

        /// <summary>
        /// 重置敌人池（在新楼层开始时调用）
        /// </summary>
        public void ResetEnemyPool()
        {
            EnemyPool.ClearDefeatedEnemies();
            Debug.Log("[CombatRoom] ResetEnemyPool: 敌人击败记录已清除");
        }

        /// <summary>
        /// 从对象池中移除当前选中的敌人（战斗胜利后调用）
        /// </summary>
        public void RemoveCurrentEnemyFromPool()
        {
            Debug.Log($"[CombatRoom] RemoveCurrentEnemyFromPool 被调用: enemyPool={(enemyPool != null ? enemyPool.DisplayName : "null")}, selectedEnemy={(selectedEnemy != null ? selectedEnemy.name : "null")}");
            
            if (enemyPool == null)
            {
                Debug.LogWarning("[CombatRoom] RemoveCurrentEnemyFromPool: enemyPool 为 null，无法移除");
                return;
            }
            
            if (selectedEnemy == null)
            {
                Debug.LogWarning("[CombatRoom] RemoveCurrentEnemyFromPool: selectedEnemy 为 null，无法移除");
                return;
            }
            
            Debug.Log($"[CombatRoom] 移除前 - EnemyPool 敌人数={enemyPool.Count}, 已击败记录数={EnemyPool.DefeatedCount}");
            
            bool removed = enemyPool.RemoveEnemy(selectedEnemy);
            
            Debug.Log($"[CombatRoom] 移除后 - 结果={removed}, EnemyPool 敌人数={enemyPool.Count}, 已击败记录数={EnemyPool.DefeatedCount}");
        }

        /// <summary>
        /// 获取当前战斗房间的敌人池引用
        /// </summary>
        public EnemyPool GetEnemyPool()
        {
            return enemyPool;
        }

        /// <summary>
        /// 从对象池随机选择敌人和对应精灵
        /// </summary>
        private void SelectEnemyFromPool()
        {
            Debug.Log("[CombatRoom] SelectEnemyFromPool 开始...");
            selectedEnemy = null;
            selectedSpirit = null;

            // 优先从对象池获取（EnemyPool会自动跳过已击败的敌人）
            if (enemyPool != null)
            {
                Debug.Log($"[CombatRoom] EnemyPool 已配置: Name={enemyPool.DisplayName}, Count={enemyPool.Count}, AvailableCount={enemyPool.AvailableCount}, IsEmpty={enemyPool.IsEmpty}, 已击败={EnemyPool.DefeatedCount}");
                
                // 打印 EnemyPool 中所有敌人
                if (enemyPool.Enemies != null)
                {
                    Debug.Log($"[CombatRoom] EnemyPool.Enemies 列表内容 (共{enemyPool.Enemies.Count}个):");
                    for (int i = 0; i < enemyPool.Enemies.Count; i++)
                    {
                        var e = enemyPool.Enemies[i];
                        Debug.Log($"  [{i}] {(e != null ? e.name : "null")}");
                    }
                }
                else
                {
                    Debug.LogWarning("[CombatRoom] EnemyPool.Enemies 列表为 null!");
                }
                
                if (!enemyPool.IsEmpty)
                {
                    var pair = useWeightedRandom
                        ? enemyPool.GetWeightedRandomEnemyWithSpirit()
                        : enemyPool.GetRandomEnemyWithSpirit();

                    selectedEnemy = pair.enemy;
                    selectedSpirit = pair.spirit;
                    Debug.Log($"[CombatRoom] 从对象池选择敌人: {(selectedEnemy != null ? selectedEnemy.name : "null")}, 精灵: {(selectedSpirit != null ? selectedSpirit.name : "null")}");
                }
                else
                {
                    Debug.LogWarning("[CombatRoom] EnemyPool 为空！");
                }
            }
            else
            {
                Debug.LogWarning("[CombatRoom] EnemyPool 未配置！");
            }

            // 回退到手动配置的 enemyData
            if (selectedEnemy == null)
            {
                Debug.Log($"[CombatRoom] 尝试使用备用 enemyData: {(enemyData != null ? enemyData.name : "null")}");
                selectedEnemy = enemyData;
                if (enemyPool != null && selectedEnemy != null)
                {
                    selectedSpirit = enemyPool.GetSpiritForEnemy(selectedEnemy);
                }
            }

            // 初始化敌人模型（用于HP/Mana判定）
            if (selectedEnemy != null)
            {
                enemyModel = new Enemy(selectedEnemy);
                Debug.Log($"[CombatRoom] 敌人模型已创建: Name={selectedEnemy.name}, HP={enemyModel.HP}, Mana={enemyModel.Mana}");
            }
            else
            {
                Debug.LogError("[CombatRoom] 未能获取敌人数据！请检查 EnemyPool 或 EnemyData 配置");
            }
        }

        /// <summary>
        /// 激活 BattleController 并开始战斗
        /// </summary>
        private void StartBattle()
        {
            Debug.Log("[CombatRoom] StartBattle 开始...");
            
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
                        Debug.Log($"[CombatRoom] 找到未激活的 BattleController: {bc.gameObject.name}");
                    }
                }
                battleController = bc; // 缓存引用
            }
            
            if (bc == null)
            {
                Debug.LogError("[CombatRoom] 未找到 BattleController！请确保场景中存在 BattleController");
                return;
            }
            
            Debug.Log($"[CombatRoom] 找到 BattleController: {bc.gameObject.name}, active={bc.gameObject.activeInHierarchy}");

            // 检查必要数据
            if (playerData == null)
            {
                Debug.LogError("[CombatRoom] PlayerData 未配置！请在 Inspector 中配置 PlayerData");
                return;
            }
            if (selectedEnemy == null)
            {
                Debug.LogError("[CombatRoom] EnemyData 未获取！请检查 EnemyPool 配置");
                return;
            }

            // 确保 BattleController 激活（OnEnable 应该已经激活了，这里做双重保险）
            if (!bc.gameObject.activeInHierarchy)
            {
                bc.gameObject.SetActive(true);
                Debug.Log("[CombatRoom] 激活 BattleController");
            }

            // 传入数据并开始战斗（同时传入自身引用，用于战斗胜利后移除敌人）
            // 注意：不要在这里调用 ShowBattlePanel()，因为 BeginBattleWith -> InitializeBattle -> battleView.Bind() 会自动激活并绑定UI
            Debug.Log($"[CombatRoom] 开始战斗: Player={playerData.name}, Enemy={selectedEnemy.name}");
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
            Debug.Log("[CombatRoom] 房间已被外部标记为清理完成");

            if (!itemDropped)
            {
                TryDropRandomItem();
                itemDropped = true;
            }
        }

        // 提供敌人模型访问，便于其他系统修改其 HP/Mana
        public Enemy GetEnemyModel() => enemyModel;

        // 提供当前敌人/精灵数据访问
        public EnemyData GetSelectedEnemyData() => selectedEnemy;
        public SpiritData GetSelectedSpiritData() => selectedSpirit;

        /// <summary>
        /// 尝试捕捉当前敌人对应的精灵
        /// </summary>
        /// <param name="earlyCapture">是否为提前捕捉（狩猎大师羁绊）</param>
        /// <returns>捕捉成功返回(true, SpiritData)，失败返回(false, null)</returns>
        public (bool success, SpiritData spirit) AttemptCapture(bool earlyCapture = false)
        {
            Debug.Log($"[CombatRoom] AttemptCapture 开始: earlyCapture={earlyCapture}, IsCleared={IsCleared()}, spiritCaptured={spiritCaptured}, selectedSpirit={(selectedSpirit != null ? selectedSpirit.name : "null")}, playerData={(playerData != null ? playerData.name : "null")}");
            
            // 检查是否已经捕捉过，避免重复捕捉
            if (spiritCaptured)
            {
                Debug.Log("[CombatRoom] 已经捕捉过精灵，跳过重复捕捉");
                return (false, null);
            }
            
            // 检查房间是否已清理（或是否为提前捕捉）
            if (!earlyCapture && !IsCleared())
            {
                Debug.Log("[CombatRoom] 房间未清理完成，无法捕捉");
                return (false, null);
            }

            // 确定要捕捉的SpiritData
            SpiritData spiritToCapture = selectedSpirit;
            if (spiritToCapture == null && enemyPool != null && selectedEnemy != null)
            {
                spiritToCapture = enemyPool.GetSpiritForEnemy(selectedEnemy);
                Debug.Log($"[CombatRoom] selectedSpirit为null，从EnemyPool获取: {(spiritToCapture != null ? spiritToCapture.name : "null")}");
            }

            if (spiritToCapture == null)
            {
                Debug.LogWarning("[CombatRoom] 无法确定要捕捉的SpiritData - selectedSpirit和EnemyPool都无法获取");
                return (false, null);
            }

            Debug.Log($"[CombatRoom] 准备捕捉精灵: {spiritToCapture.DisplayName}");

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
                    Debug.Log($"[CombatRoom] {captureType}: {spiritToCapture.DisplayName}");
                    return (true, spiritToCapture);
                }
            }
            else
            {
                Debug.LogWarning("[CombatRoom] PlayerManager.Instance为null，尝试直接修改PlayerData");

                // 降级方案：直接修改PlayerData（仅编辑器可见）
                if (playerData != null)
                {
                    var owned = playerData.GetOwnedSpirits();
                    int beforeCount = owned.Count;
                    owned.Add(spiritToCapture);
                    playerData.OwnedSpirits = owned.ToArray();

                    spiritCaptured = true;

                    Debug.Log($"[CombatRoom] 捕捉成功（仅PlayerData）! 精灵={spiritToCapture.DisplayName}, 拥有精灵数: {beforeCount} -> {playerData.OwnedSpirits.Length}");

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                    string captureType = earlyCapture ? "提前捕捉成功（诱捕）" : "捕捉成功";
                    Debug.Log($"[CombatRoom] {captureType}: {spiritToCapture.DisplayName}");
                    return (true, spiritToCapture);
                }
            }

            Debug.LogWarning("[CombatRoom] PlayerManager和PlayerData都无法使用，捕捉失败");
            return (false, null);
        }
        public override void ExitRoom()
        {
            GrantPendingDrop();
            DestroyDropPickupInstance();

            // 捕捉精灵：离开房间时，如果已清理敌人，则将对应SpiritData添加到玩家拥有列表
            if (playerData == null)
            {
                Debug.LogWarning("[CombatRoom] PlayerData 未配置，无法捕捉精灵");
                return;
            }

            // 检查是否已经捕捉过，避免重复捕捉
            if (spiritCaptured)
            {
                Debug.Log("[CombatRoom] ExitRoom: 已经捕捉过精灵，跳过重复捕捉");
                return;
            }

            // 仅在房间清理完成（敌人被击败或实例销毁）时执行捕捉
            if (!IsCleared())
            {
                Debug.Log("[CombatRoom] 房间未清理完成，不执行捕捉精灵");
                return;
            }

            // 确定要添加的SpiritData：优先用生成时绑定的 selectedSpirit
            SpiritData spiritToAdd = selectedSpirit;
            if (spiritToAdd == null && enemyPool != null && selectedEnemy != null)
            {
                spiritToAdd = enemyPool.GetSpiritForEnemy(selectedEnemy);
            }
            if (spiritToAdd == null)
            {
                Debug.LogWarning("[CombatRoom] 无法确定要捕捉的SpiritData（可能对象池/映射未配置）");
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
                    Debug.Log($"[CombatRoom] ExitRoom 捕捉精灵成功: {spiritToAdd.DisplayName}");
                }
            }
            else
            {
                Debug.LogWarning("[CombatRoom] ExitRoom: PlayerManager.Instance为null，尝试直接修改PlayerData");

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
                    Debug.Log($"[CombatRoom] ExitRoom 捕捉精灵成功（仅PlayerData）: {spiritToAdd.DisplayName}");
                }
            }
        }

        // 击败敌人时从对象池随机掉落一个道具，并写入背包与玩家数据
        private void TryDropRandomItem()
        {
            if (itemPool == null || itemPool.IsEmpty)
            {
                Debug.LogWarning("[CombatRoom] itemPool 未配置或为空，无法掉落道具");
                return;
            }

            var item = useWeightedRandom ? itemPool.GetWeightedRandomItem() : itemPool.GetRandomItem();
            if (item == null)
            {
                Debug.LogWarning("[CombatRoom] 未能从对象池抽到掉落道具");
                return;
            }

            pendingDropItem = item;
            pendingDropQuantity = Mathf.Max(1, dropQuantity);
            pendingDropGranted = false;
            Debug.Log($"[CombatRoom] 掉落生成待领取: {item.DisplayName} x{pendingDropQuantity}");

            RecordDropToPlayerData(item, pendingDropQuantity);
            pendingDropGranted = TryAddDropToInventoryImmediately(item, pendingDropQuantity);

            SpawnDropPickupVisual();
        }

        private void RecordDropToPlayerData(ItemData item, int quantity)
        {
            if (playerData == null)
            {
                Debug.LogWarning("[CombatRoom] PlayerData 未配置，掉落无法写入玩家已有道具");
                return;
            }

            var list = playerData.GetInitialItems();
            list.Add(new ItemStackConfig { Item = item, Count = Mathf.Max(1, quantity) });
            playerData.InitialItems = list.ToArray();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(playerData);
#endif
            Debug.Log($"[CombatRoom] 掉落已立即写入玩家已有道具: {item.DisplayName} x{Mathf.Max(1, quantity)}");
        }

        private void SpawnDropPickupVisual()
        {
            if (pickupPrefab == null || pendingDropItem == null)
            {
                Debug.Log("[CombatRoom] SpawnDropPickupVisual skipped: pickupPrefab或掉落道具未配置");
                return;
            }

            DestroyDropPickupInstance();

            Vector3 spawnPos;
            Quaternion spawnRot;
            Transform parent;

            if (spawnPoint != null)
            {
                spawnPos = spawnPoint.position;
                spawnRot = spawnPoint.rotation;
                parent = spawnPoint;
            }
            else
            {
                spawnPos = ResolveRoomCenter();
                spawnRot = transform.rotation;
                parent = transform;
            }

            dropPickupInstance = Instantiate(pickupPrefab, spawnPos, spawnRot, parent);
            Debug.Log($"[CombatRoom] Spawned drop pickup visual -> {dropPickupInstance.name} at {(spawnPoint != null ? spawnPoint.name : "RoomCenter")}");

            var display = dropPickupInstance.GetComponent<ItemDisplay>();
            if (display != null)
            {
                display.item = pendingDropItem;
                display.Refresh();
                Debug.Log("[CombatRoom] ItemDisplay refreshed for drop pickup");
            }
        }

        private Vector3 ResolveRoomCenter()
        {
            var collider3D = GetComponentInChildren<Collider>();
            if (collider3D != null)
            {
                return collider3D.bounds.center;
            }

            var collider2D = GetComponentInChildren<Collider2D>();
            if (collider2D != null)
            {
                return collider2D.bounds.center;
            }

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center;
            }

            return transform.position;
        }

        private bool TryAddDropToInventoryImmediately(ItemData item, int quantity)
        {
            var inv = InventoryManager.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[CombatRoom] 未找到 InventoryManager.Instance，掉落暂未加入背包");
                return false;
            }

            bool ok = inv.AddItem(item, quantity);
            if (!ok)
            {
                Debug.LogWarning("[CombatRoom] 掉落加入背包失败（可能背包已满），将在离开房间时重试");
            }
            else
            {
                Debug.Log($"[CombatRoom] 掉落已立即加入背包: {item.DisplayName} x{quantity}");
            }
            return ok;
        }

        private void GrantPendingDrop()
        {
            if (pendingDropItem == null || pendingDropGranted)
            {
                // 如果道具已添加到背包，仍需清理展示与引用
                pendingDropItem = null;
                pendingDropQuantity = 0;
                pendingDropGranted = false;
                DestroyDropPickupInstance();
                return;
            }

            int grantCount = Mathf.Max(1, pendingDropQuantity);

            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                bool ok = inv.AddItem(pendingDropItem, grantCount);
                if (!ok)
                {
                    Debug.LogWarning("[CombatRoom] 掉落加入背包失败，可能背包已满或参数非法");
                }
                else
                {
                    Debug.Log($"[CombatRoom] 掉落已加入背包: {pendingDropItem.DisplayName} x{grantCount}");
                    pendingDropGranted = true;
                }
            }
            else
            {
                Debug.LogWarning("[CombatRoom] 未找到 InventoryManager.Instance，背包未更新");
            }

            if (pendingDropGranted)
            {
                pendingDropItem = null;
                pendingDropQuantity = 0;
            }

            DestroyDropPickupInstance();
            pendingDropGranted = false;
        }

        private void HandleRoomCompleted(RoomNode_cza node)
        {
            if (node == null || node.Type != RoomType_cza.Combat)
                return;

            GrantPendingDrop();
            DestroyDropPickupInstance();
        }

        private void HandleBranchChoicesUpdated(IReadOnlyList<int> _)
        {
            DestroyDropPickupInstance();
        }

        private void SubscribeRoomCompletedEvent()
        {
            var sm = RoomStateMachine_cza.Instance;
            if (sm != null)
            {
                if (!subscribedRoomCompleted)
                {
                    sm.OnRoomCompleted += HandleRoomCompleted;
                    subscribedRoomCompleted = true;
                }
                if (!subscribedBranchChoices)
                {
                    sm.OnBranchChoicesUpdated += HandleBranchChoicesUpdated;
                    subscribedBranchChoices = true;
                }
            }
            else if (waitRoomStateCo == null)
            {
                waitRoomStateCo = StartCoroutine(WaitForRoomStateMachine());
            }
        }

        private void UnsubscribeRoomCompletedEvent()
        {
            if (waitRoomStateCo != null)
            {
                StopCoroutine(waitRoomStateCo);
                waitRoomStateCo = null;
            }

            var sm = RoomStateMachine_cza.Instance;
            if (sm != null && subscribedRoomCompleted)
            {
                sm.OnRoomCompleted -= HandleRoomCompleted;
                subscribedRoomCompleted = false;
            }
            if (sm != null && subscribedBranchChoices)
            {
                sm.OnBranchChoicesUpdated -= HandleBranchChoicesUpdated;
                subscribedBranchChoices = false;
            }
        }

        private IEnumerator WaitForRoomStateMachine()
        {
            yield return new WaitUntil(() => RoomStateMachine_cza.Instance != null);
            waitRoomStateCo = null;
            SubscribeRoomCompletedEvent();
        }

        private void DestroyDropPickupInstance()
        {
            if (dropPickupInstance == null)
                return;

            Destroy(dropPickupInstance);
            dropPickupInstance = null;
            Debug.Log("[CombatRoom] Drop pickup instance destroyed");
        }

        public void CleanupDropVisual()
        {
            DestroyDropPickupInstance();
        }
    }

    
}

// x