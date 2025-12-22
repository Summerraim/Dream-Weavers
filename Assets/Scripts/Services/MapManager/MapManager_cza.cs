
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager_cza : MonoBehaviour
{
    [SerializeField] private int startFloor = 1;
    [Header("Combat EnemyPools (floors 1-4)")]
    [Tooltip("Switches CombatRoom.enemyPool based on the current floor. Index 0 => floor 1.")]
    [SerializeField] private EnemyPool[] combatEnemyPoolsByFloor = new EnemyPool[4];
    // [SerializeField] private Text roomInfoText;
    private bool subscribed;

    private IEnumerator Start()
    {
        // 纭繚瀛樺湪 SeedManager锛岃嫢娌℃湁鍒欏垱寤轰竴涓粯璁ょ殑
        if (SeedManager_cza.Instance == null)
        {
            var seedGo = new GameObject("SeedManager_cza");
            seedGo.AddComponent<SeedManager_cza>();
            Debug.Log("[MapManager] 鍒涘缓榛樿 SeedManager_cza");
            // 绛夊緟 Awake 瀹屾垚浠ヨ缃?Instance 涓?RNG
            yield return null;
        }

        if (RoomStateMachine_cza.Instance == null)
        {
            var go = new GameObject("RoomStateMachine");
            go.AddComponent<RoomStateMachine_cza>();
            Debug.Log("[MapManager] 鍒涘缓 RoomStateMachine_cza");
            yield return null; // 绛夊緟 Awake/Start
        }

        if (!subscribed)
        {
            RoomStateMachine_cza.Instance.OnRoomEntered += OnRoomEntered;
            RoomStateMachine_cza.Instance.OnFloorPreparing += OnFloorPreparing;
            subscribed = true;
        }

        if (RoomStateMachine_cza.Instance.CurrentMap == null)
        {
            RoomStateMachine_cza.Instance.InitFloor(startFloor);
            Debug.Log($"[MapManager] 鍒濆鍖栨ゼ灞?{startFloor}");
        }
        else
        {
            Debug.Log("[MapManager] 妫€娴嬪埌妤煎眰宸插垵濮嬪寲锛岃烦杩囦簩娆″垵濮嬪寲");
        }
        if (RoomStateMachine_cza.Instance.CurrentMap != null)
        {
            ApplyCombatEnemyPoolForFloor(RoomStateMachine_cza.Instance.CurrentMap.FloorIndex);
        }
    }

    private void OnDisable()
    {
        if (subscribed && RoomStateMachine_cza.Instance != null)
        {
            RoomStateMachine_cza.Instance.OnRoomEntered -= OnRoomEntered;
            RoomStateMachine_cza.Instance.OnFloorPreparing -= OnFloorPreparing;
            subscribed = false;
        }
    }

    private void OnRoomEntered(RoomNode_cza node)
    {
        Debug.Log($"[MapManager] 杩涘叆鎴块棿 Id={node.Id} Type={node.Type}");
    }

    private void OnFloorPreparing(int floor)
    {
        ApplyCombatEnemyPoolForFloor(floor);
    }

    private void ApplyCombatEnemyPoolForFloor(int floor)
    {
        if (combatEnemyPoolsByFloor == null || combatEnemyPoolsByFloor.Length == 0)
        {
            Debug.LogWarning("[MapManager] combatEnemyPoolsByFloor 鏈厤缃紝鏃犳硶鍒囨崲 CombatRoom enemyPool");
            return;
        }

        int idx = Mathf.Clamp(floor - 1, 0, combatEnemyPoolsByFloor.Length - 1);
        EnemyPool pool = combatEnemyPoolsByFloor[idx];
        if (pool == null)
        {
            Debug.LogWarning($"[MapManager] 绗瑊floor灞傜殑 EnemyPool 鏈厤缃紙combatEnemyPoolsByFloor[{idx}]=null锛夛紝灏嗙户缁娇鐢?CombatRoom 褰撳墠 enemyPool");
            return;
        }

        var combat = FindObjectOfType<DreamWeavers.Rooms.CombatRoom_cza>();
        if (combat == null)
        {
            var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.CombatRoom_cza>();
            if (all != null && all.Length > 0)
            {
                combat = all[0];
            }
        }

        if (combat == null)
        {
            Debug.LogWarning("[MapManager] 鏈壘鍒?CombatRoom_cza锛屾棤娉曞垏鎹?enemyPool");
            return;
        }

        combat.SetEnemyPool(pool);
        Debug.Log($"[MapManager] 宸插垏鎹?CombatRoom enemyPool -> Floor={floor}, Pool={(string.IsNullOrEmpty(pool.DisplayName) ? pool.name : pool.DisplayName)}");
    }}

