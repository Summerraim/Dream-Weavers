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

        // 运行时状态
        private GameObject enemyInstance;
        private bool spawned = false;
        private Enemy enemyModel; // 敌人模型（非 MonoBehaviour），来自 EnemyModel

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

            enemyInstance = Instantiate(enemyPrefab, sp.position, sp.rotation);
            spawned = true;

            // 初始化敌人模型数据（用于HP/Mana等判定）
            // 直接使用房间中配置的 ScriptableObject 敌人数据
            if (enemyData != null)
            {
                enemyModel = new Enemy(enemyData);
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
            if (enemyData == null)
            {
                Debug.LogWarning("[CombatRoom] 未配置 EnemyData，无法开始战斗");
                return;
            }
            bc.BeginBattleWith(playerData, enemyData);
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
        public override void ExitRoom()
        {
            // 离开房间时无需额外处理，可按需扩展
        }
    }

    
}
