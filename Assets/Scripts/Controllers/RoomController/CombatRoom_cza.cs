using UnityEngine;
 

namespace DreamWeavers.Rooms
{
    // 战斗房间（精简版）：进入房间后在固定点生成一个不移动的敌人
    // 继承 RoomBase_cza，以保持与现有房间系统一致
    public class CombatRoom_cza : RoomBase_cza
    {
        [Header("生成相关")]
        [SerializeField] private Transform spawnPoint; // 固定出生点
        [SerializeField] private GameObject enemyPrefab; // 敌人预制体（单个）
        [SerializeField] private EnemyData enemyData; // 用于初始化敌人模型（可选）
        [SerializeField] private PlayerData playerData; // 当前玩家数据（用于传递到战斗）

        [Header("对象池与随机配置")]
        [SerializeField] private EnemyPool enemyPool; // 敌人对象池（含敌人与对应精灵）
        [SerializeField] private bool useWeightedRandom = false; // 是否使用权重随机

        // 运行时状态
        private GameObject enemyInstance;
        private bool spawned = false;
        private Enemy enemyModel; // 敌人模型（非 MonoBehaviour），来自 EnemyModel
        private SpiritData selectedSpirit; // 与当前敌人对应的精灵数据
        private EnemyData selectedEnemy; // 当前选中的敌人数据（来自对象池或手动配置）

        public override void EnterRoom()
        {
            // 进入房间即生成一个敌人（仅一次）
            TrySpawnEnemy();

            // 将玩家与敌人数据传递给战斗控制器并初始化战斗
            TryStartBattle();
        }

        private void TrySpawnEnemy()
        {
            if (spawned) return;

            var sp = spawnPoint != null ? spawnPoint : transform; // 未配置则用房间中心
            if (enemyPrefab == null)
            {
                Debug.LogWarning("[CombatRoom] 未配置敌人预制体");
                return;
            }
            // 通过对象池选取敌人与对应精灵（优先对象池）
            selectedEnemy = null;
            selectedSpirit = null;
            if (enemyPool != null && !enemyPool.IsEmpty)
            {
                var pair = useWeightedRandom
                    ? enemyPool.GetWeightedRandomEnemyWithSpirit()
                    : enemyPool.GetRandomEnemyWithSpirit();

                selectedEnemy = pair.enemy;
                selectedSpirit = pair.spirit;
            }

            // 若未从对象池取到，则回退到手动配置的 enemyData
            if (selectedEnemy == null)
            {
                selectedEnemy = enemyData;
                if (selectedEnemy == null)
                {
                    Debug.LogWarning("[CombatRoom] 未能获取到敌人数据（对象池为空或未配置，且未提供手动 EnemyData）");
                }
                // 尝试根据敌人从对象池获取对应精灵（如果池存在且包含该敌人）
                if (enemyPool != null && selectedEnemy != null)
                {
                    selectedSpirit = enemyPool.GetSpiritForEnemy(selectedEnemy);
                }
            }

            // 实例化敌人
            enemyInstance = Instantiate(enemyPrefab, sp.position, sp.rotation);
            spawned = true;

            // 初始化敌人模型数据（用于HP/Mana等判定）
            if (selectedEnemy != null)
            {
                enemyModel = new Enemy(selectedEnemy);
            }
        }

        private void TryStartBattle()
        {
            var bc = GameObject.FindObjectOfType<BattleController>();
            if (bc == null)
            {
                Debug.LogWarning("[CombatRoom] 未找到 BattleController，无法开始战斗");
                return;
            }
            if (playerData == null)
            {
                Debug.LogWarning("[CombatRoom] 未配置 PlayerData，无法开始战斗");
                return;
            }
            if (selectedEnemy == null)
            {
                Debug.LogWarning("[CombatRoom] 未配置或选取 EnemyData，无法开始战斗");
                return;
            }
            // 将从对象池选取的敌人数据传递到战斗控制器
            bc.BeginBattleWith(playerData, selectedEnemy);
        }

        public bool IsCleared()
        {
            // 房间清理判定：如果已生成且该敌人实例被销毁（死亡），则视为清理完成
            if (!spawned) return false;
            if (enemyInstance == null) return true;
            if (enemyModel != null)
                return enemyModel.HP <= 0 || enemyModel.Mana <= 0;
            return false;
        }

        // 提供敌人模型访问，便于其他系统修改其 HP/Mana
        public Enemy GetEnemyModel() => enemyModel;

        // 提供当前敌人/精灵数据访问
        public EnemyData GetSelectedEnemyData() => selectedEnemy;
        public SpiritData GetSelectedSpiritData() => selectedSpirit;
        public override void ExitRoom()
        {
            // 捕捉精灵：离开房间时，如果已清理敌人，则将对应SpiritData添加到玩家拥有列表
            if (playerData == null)
            {
                Debug.LogWarning("[CombatRoom] PlayerData 未配置，无法捕捉精灵");
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

            // 添加到 PlayerData.OwnedSpirits（数组）——允许重复捕捉
            var owned = playerData.GetOwnedSpirits();
            owned.Add(spiritToAdd);
            playerData.OwnedSpirits = owned.ToArray();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(playerData);
#endif
            Debug.Log($"[CombatRoom] 捕捉精灵成功: {spiritToAdd.DisplayName} (允许重复)");
        }
    }

    
}

// x