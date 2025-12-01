using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager_cza : MonoBehaviour
{
    [SerializeField] private int startFloor = 1;
    // [SerializeField] private Text roomInfoText;
    private bool subscribed;

    private IEnumerator Start()
    {
        // 确保存在 SeedManager，若没有则创建一个默认的
        if (SeedManager_cza.Instance == null)
        {
            var seedGo = new GameObject("SeedManager_cza");
            seedGo.AddComponent<SeedManager_cza>();
            Debug.Log("[MapManager] 创建默认 SeedManager_cza");
            // 等待 Awake 完成以设置 Instance 与 RNG
            yield return null;
        }

        if (RoomStateMachine_cza.Instance == null)
        {
            var go = new GameObject("RoomStateMachine");
            go.AddComponent<RoomStateMachine_cza>();
            Debug.Log("[MapManager] 创建 RoomStateMachine_cza");
            yield return null; // 等待 Awake/Start
        }

        if (!subscribed)
        {
            RoomStateMachine_cza.Instance.OnRoomEntered += OnRoomEntered;
            subscribed = true;
        }

        if (RoomStateMachine_cza.Instance.CurrentMap == null)
        {
            RoomStateMachine_cza.Instance.InitFloor(startFloor);
            Debug.Log($"[MapManager] 初始化楼层 {startFloor}");
        }
        else
        {
            Debug.Log("[MapManager] 检测到楼层已初始化，跳过二次初始化");
        }
    }

    private void OnDisable()
    {
        if (subscribed && RoomStateMachine_cza.Instance != null)
        {
            RoomStateMachine_cza.Instance.OnRoomEntered -= OnRoomEntered;
            subscribed = false;
        }
    }

    private void OnRoomEntered(RoomNode_cza node)
    {
        Debug.Log($"[MapManager] 进入房间 Id={node.Id} Type={node.Type}");
    }
}
