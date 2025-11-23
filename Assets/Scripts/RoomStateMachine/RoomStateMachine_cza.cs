using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomStateMachine_cza : MonoBehaviour
{
    public static RoomStateMachine_cza Instance;

    public FloorMap_cza CurrentMap { get; private set; }
    public RoomNode_cza CurrentRoom { get; private set; }

    public event Action<RoomNode_cza> OnRoomEntered;
    public event Action<RoomNode_cza> OnRoomCompleted;

    // 新增：是否处于“等待玩家选择分支”的状态
    private bool awaitingChoice;

    private HashSet<int> visitedRooms = new HashSet<int>();
    private List<int> branchChoices = new List<int>();

    public IReadOnlyList<int> GetCurrentBranchChoices() => branchChoices;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<int> GetUnvisitedRoomIds()
    {
        var list = new List<int>();
        if (CurrentMap == null) return list;
        foreach (var kv in CurrentMap.Rooms)
            if (!visitedRooms.Contains(kv.Key))
                list.Add(kv.Key);
        return list;
    }

    // 初始化指定楼层
    public void InitFloor(int floor)
    {
        var rng = SeedManager_cza.Instance.RNG;
        visitedRooms.Clear();          // 调整：先清空
        branchChoices.Clear();         // 调整：先清空
        CurrentMap = MapGenerator_cza.GenerateFloor(floor, rng);
        EnterRoom(1); // 默认进入首房（已被记录为 visited）
        Debug.Log($"[RoomState] 初始化楼层 {floor}");
    }

    // 进入指定房间
    public void EnterRoom(int roomId)
    {
        if (CurrentMap == null)
        {
            Debug.LogWarning("[RoomState] CurrentMap 为空，无法进入房间 " + roomId);
            return;
        }
        if (!CurrentMap.Rooms.ContainsKey(roomId))
        {
            Debug.LogWarning($"[RoomState] Rooms 不包含键 {roomId} (当前键集合={string.Join(",", CurrentMap.Rooms.Keys)})");
            return;
        }
        CurrentRoom = CurrentMap.Rooms[roomId];
        // 每次进入房间都退出选择模式，避免误触
        awaitingChoice = false;
        visitedRooms.Add(roomId);
        branchChoices.Clear();
        Debug.Log($"[RoomState] Enter Room {roomId} Type={CurrentRoom.Type}");
        Debug.Log($"[RoomState] 静态前向分支(基础图)={ (CurrentRoom.NextRooms!=null? string.Join(",", CurrentRoom.NextRooms):"无") }");
        Debug.Log("[RoomState] 真实可选分支将在房间完成后生成");
        HandleRoomEnter(CurrentRoom);
        OnRoomEntered?.Invoke(CurrentRoom);
    }

    // 当前房间完成（战斗胜利/事件结束等）
    public void CompleteCurrentRoom()
    {
        if (CurrentRoom == null) return;
        Debug.Log($"[RoomState] Complete Room {CurrentRoom.Id}");
        OnRoomCompleted?.Invoke(CurrentRoom);

        // 完成后进入选择模式（若有后续房间）
        // Boss 房直接结束
        if (CurrentRoom.Type == RoomType_cza.Boss)
        {
            awaitingChoice = false;
            Debug.Log("[RoomState] Boss 房完成，楼层结束");
            return;
        }

        branchChoices.Clear();
        // 1. 添加当前房间的前向分支（未访问）
        var forward = CurrentRoom.NextRooms;
        if (forward != null)
        {
            foreach (var id in forward)
            {
                if (id == CurrentRoom.Id) continue;
                if (visitedRooms.Contains(id)) continue;
                if (!branchChoices.Contains(id))
                    branchChoices.Add(id);
                if (branchChoices.Count >= 3) break;
            }
        }

        // 2. 若不足3条，用“编号小于当前的未访问房间（被跳过的中间房间）”填充
        if (branchChoices.Count < 3)
        {
            // 按升序补齐
            for (int id = 1; id < CurrentRoom.Id && branchChoices.Count < 3; id++)
            {
                if (id == CurrentRoom.Id) continue;
                if (visitedRooms.Contains(id)) continue;
                if (!CurrentMap.Rooms.ContainsKey(id)) continue;
                if (branchChoices.Contains(id)) continue;
                branchChoices.Add(id);
            }
        }

        // 3. 仍不足再用“编号大于当前的其它未访问房间”填充（避免出现只有2条时）
        if (branchChoices.Count < 3)
        {
            for (int id = CurrentRoom.Id + 1; id <= CurrentMap.Rooms.Count && branchChoices.Count < 3; id++)
            {
                if (visitedRooms.Contains(id)) continue;
                if (!CurrentMap.Rooms.ContainsKey(id)) continue;
                if (branchChoices.Contains(id)) continue;
                branchChoices.Add(id);
            }
        }

        awaitingChoice = branchChoices.Count > 0;
        if (awaitingChoice)
        {
            Debug.Log($"[RoomState] 可选路线(未访问房间): {string.Join(",", branchChoices)}");
            for (int i = 0; i < branchChoices.Count; i++)
                Debug.Log($"[RoomState] 键 {i+1} -> 房间 {branchChoices[i]}");
        }
        else
        {
            Debug.Log("[RoomState] 无可选未访问房间，流程结束或等待其它逻辑");
        }
    }

    // 新增：在选择模式下监听玩家输入（键盘 1/2/3 或小键盘 1/2/3）
    private void Update()
    {
        if (!awaitingChoice) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            ChooseNextByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            ChooseNextByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            ChooseNextByIndex(2);
        }
    }

    private void ChooseNextByIndex(int idx)
    {
        if (branchChoices.Count == 0) { Debug.LogWarning("[RoomState] 无可选分支"); return; }
        int raw = idx;
        idx = Mathf.Clamp(idx, 0, branchChoices.Count - 1);
        int target = branchChoices[idx];
        awaitingChoice = false;
        Debug.Log($"[RoomState] 选择分支 输入={raw} 修正后={idx} -> 房间 {target}");
        EnterRoom(target);
    }

    // 走向分支中的下一个房间
    public void GoToNext(int choiceIndex)
    {
        // 替换旧实现：直接复用 ChooseNextByIndex
        ChooseNextByIndex(choiceIndex);
    }

    // 根据房间类型执行进入时处理（可在此扩展生成怪物/刷新 UI 等）
    private void HandleRoomEnter(RoomNode_cza room)
    {
        Debug.Log($"[RoomState] HandleRoomEnter 房间 {room.Id} 类型 {room.Type}");
        switch (room.Type)
        {
            case RoomType_cza.Combat:
                Debug.Log("[RoomState] 准备战斗初始化");
                // TODO: 触发战斗初始化
                break;
            case RoomType_cza.Rest:
                Debug.Log("[RoomState] 休息房：恢复玩家状态");
                // TODO: 恢复玩家状态
                break;
            case RoomType_cza.Props:
                Debug.Log("[RoomState] 道具房：刷新商店/掉落");
                // TODO: 刷新道具商店或掉落
                break;
            case RoomType_cza.Events:
                Debug.Log("[RoomState] 事件房：触发事件");
                // TODO
                break;
            case RoomType_cza.Boss:
                Debug.Log("[RoomState] Boss 房：初始化 Boss 战");
                // TODO: 触发Boss战
                break;
        }
    }
}
