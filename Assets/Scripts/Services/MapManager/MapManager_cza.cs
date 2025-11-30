using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager_cza : MonoBehaviour
{
    [SerializeField] private int startFloor = 1;
    // [SerializeField] private Text roomInfoText;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => SeedManager_cza.Instance != null);

        if (RoomStateMachine_cza.Instance == null)
        {
            var go = new GameObject("RoomStateMachine");
            go.AddComponent<RoomStateMachine_cza>();
            Debug.Log("[MapManager] 创建 RoomStateMachine_cza");
            yield return null;
        }
        RoomStateMachine_cza.Instance.OnRoomEntered += OnRoomEntered;
        RoomStateMachine_cza.Instance.InitFloor(startFloor);
        Debug.Log($"[MapManager] 初始化楼层 {startFloor}");
    }

    private void OnRoomEntered(RoomNode_cza node)
    {
        Debug.Log("Entered room " + node.Id);
        var next = (node.NextRooms != null && node.NextRooms.Count > 0)
            ? string.Join(",", node.NextRooms)
            : "无";
        Debug.Log($"[MapManager] 进入房间 Id={node.Id} Type={node.Type} 后继={next}");
        Debug.Log($"[MapManager] 静态前向分支(NextRooms): {next}");
        var unvisited = RoomStateMachine_cza.Instance.GetUnvisitedRoomIds();
        Debug.Log($"[MapManager] 剩余未访问房间: {(unvisited.Count>0 ? string.Join(",", unvisited) : "无")}");
        var choices = RoomStateMachine_cza.Instance.GetCurrentBranchChoices();
        if (choices != null && choices.Count > 0)
            Debug.Log($"[MapManager] 实际可选分支(branchChoices): {string.Join(",", choices)}");
    }
}
