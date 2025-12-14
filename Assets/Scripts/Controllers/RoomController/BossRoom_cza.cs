using UnityEngine;

namespace DreamWeavers.Rooms
{
    /// <summary>
    /// Boss房间：进入房间后生成Boss敌人
    /// 继承 RoomBase_cza，以保持与现有房间系统一致
    /// </summary>
    public class BossRoom_cza : RoomBase_cza
    {
        [Header("Boss生成相关")]
        [SerializeField] private Transform bossSpawnPoint; // Boss出生点
        [SerializeField] private GameObject bossPrefab; // Boss预制体
        [SerializeField] private EnemyData bossData; // Boss数据
        
        [Header("Boss战斗设置")]
        [SerializeField] private PlayerData playerData; // 当前玩家数据
        
        // 运行时状态
        private GameObject bossInstance;
        private bool spawned = false;
        private Enemy bossModel;

        private void Awake()
        {
            Type = RoomType_cza.Boss;
        }

        public override void EnterRoom()
        {
            // 进入房间即生成Boss（仅一次）
            TrySpawnBoss();

            // 将玩家与Boss数据传递给战斗控制器并初始化战斗
            TryStartBossBattle();
            
            Debug.Log("进入Boss房间！准备迎接挑战！");
        }

        private void TrySpawnBoss()
        {
            if (spawned) return;

            var sp = bossSpawnPoint != null ? bossSpawnPoint : transform;
            if (bossPrefab == null)
            {
                Debug.LogWarning("[BossRoom] 未配置Boss预制体");
                return;
            }

            // 实例化Boss
            bossInstance = Instantiate(bossPrefab, sp.position, sp.rotation);
            spawned = true;

            // 初始化Boss模型数据
            if (bossData != null)
            {
                bossModel = new Enemy(bossData);
                Debug.Log($"Boss生成完成: {bossData.DisplayName}, HP: {bossModel.HP}, Mana: {bossModel.Mana}");
            }
            else
            {
                Debug.LogWarning("[BossRoom] 未配置Boss数据");
            }
        }

        private void TryStartBossBattle()
        {
            var bc = GameObject.FindObjectOfType<BattleController>();
            if (bc == null)
            {
                Debug.LogWarning("[BossRoom] 未找到 BattleController，无法开始Boss战斗");
                return;
            }
            if (playerData == null)
            {
                Debug.LogWarning("[BossRoom] 未配置 PlayerData，无法开始Boss战斗");
                return;
            }
            if (bossData == null)
            {
                Debug.LogWarning("[BossRoom] 未配置 BossData，无法开始Boss战斗");
                return;
            }
            
            // 开始Boss战斗
            bc.BeginBattleWith(playerData, bossData);
            Debug.Log("Boss战斗开始！");
        }

        public bool IsBossDefeated()
        {
            // Boss被击败判定
            if (!spawned) return false;
            if (bossInstance == null) return true;
            if (bossModel != null)
                return bossModel.HP <= 0 || bossModel.Mana <= 0;
            return false;
        }

        // 提供Boss模型访问
        public Enemy GetBossModel() => bossModel;

        public override void ExitRoom()
        {
            // Boss房间离开时的特殊处理
            if (IsBossDefeated())
            {
                Debug.Log("Boss已被击败！可以前往下一个楼层。");
                // 这里可以触发Boss击败事件或奖励
            }
            else
            {
                Debug.Log("离开Boss房间，但Boss尚未被击败");
            }
        }
    }
}
