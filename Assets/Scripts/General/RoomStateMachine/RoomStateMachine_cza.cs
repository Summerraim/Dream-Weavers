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
    // 新增：当可选分支列表变化时通知 UI
    public event Action<IReadOnlyList<int>> OnBranchChoicesUpdated;
    // 新增：初始化完成（进入首房后）可交互通知
    public event Action OnReady;
    // 新增：楼层初始化完成事件（用于UI在跨楼层时做清理/隐藏）
    public event Action<int> OnFloorInitialized;

    // 新增：是否处于“等待玩家选择分支”的状态
    private bool awaitingChoice;
    public bool IsAwaitingChoice => awaitingChoice;

    private HashSet<int> visitedRooms = new HashSet<int>();
    private List<int> branchChoices = new List<int>();
    // 调试：持久记录本楼层内访问过的房间顺序，用于确认是否被意外清空
    private List<int> visitedOrderDebug = new List<int>();
    private int floorInitCounter = 0; // 记录楼层初始化次数，判断是否发生了意外重新初始化

    public IReadOnlyList<int> GetCurrentBranchChoices() => branchChoices;
    public bool IsReady => CurrentMap != null && CurrentRoom != null;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"[RoomState] 发现重复实例，销毁当前 {name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[RoomState] Awake 实例创建: {name} (visitedRooms.Count={visitedRooms.Count})");
    }

    [Header("启动配置")]
    [SerializeField] private bool autoInit = false; // 由 MapManager 负责初始化，避免重复
    [SerializeField] private int startFloor = 1;

    private void Start()
    {
        if (autoInit && CurrentMap == null)
        {
            Debug.Log($"[RoomState] AutoInit 启动，初始化楼层 {startFloor}");
            InitFloor(startFloor);
        }
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
        var rng = SeedManager_cza.Instance != null ? SeedManager_cza.Instance.RNG : null;
        Debug.Log($"[RoomState] InitFloor({floor}) 调用，floorInitCounter={floorInitCounter} -> {floorInitCounter + 1}; SeedManager={(SeedManager_cza.Instance!=null)} RNG={(rng!=null)}");
        floorInitCounter++;
        Debug.Log($"[RoomState] 清空访问集，之前数量={visitedRooms.Count} 内容=[{string.Join(",", visitedRooms)}]");
        visitedRooms.Clear();
        visitedOrderDebug.Clear();
        branchChoices.Clear();
        
        // 重置敌人池（新楼层开始时恢复所有敌人为可用）
        ResetCombatRoomEnemyPool();
        
        CurrentMap = MapGenerator_cza.GenerateFloor(floor, rng);
        EnsureAtLeastOnePropsRoom(CurrentMap, rng);
        int roomsCount = CurrentMap != null && CurrentMap.Rooms != null ? CurrentMap.Rooms.Count : -1;
        Debug.Log($"[RoomState] 楼层生成完成: map={(CurrentMap!=null)} roomsCount={roomsCount}");
        // 选择有效的起始房间：优先 1，否则取最小可用 Id
        int startId = 1;
        if (CurrentMap == null || CurrentMap.Rooms == null || CurrentMap.Rooms.Count == 0)
        {
            Debug.LogError("[RoomState] 楼层生成失败，Rooms 为空");
        }
        else if (!CurrentMap.Rooms.ContainsKey(1))
        {
            // 取字典中最小的 key 作为起点
            foreach (var kv in CurrentMap.Rooms)
            {
                if (kv.Key < startId || !CurrentMap.Rooms.ContainsKey(startId)) startId = kv.Key;
            }
            Debug.LogWarning($"[RoomState] 起始房 1 不存在，改用房间 {startId} 作为起点");
        }
        Debug.Log($"[RoomState] 计划进入起始房 startId={startId}");
        EnterRoom(startId); // 进入起始房（记录为 visited）
        Debug.Log($"[RoomState] 初始化楼层 {floor} 完成，CurrentRoom={(CurrentRoom!=null ? CurrentRoom.Id.ToString() : "null")}");
        // 通知：楼层初始化完成（用于UI隐藏上一层残留）
        try { OnFloorInitialized?.Invoke(floor); } catch (Exception ex) { Debug.LogError($"[RoomState] OnFloorInitialized 异常: {ex}"); }
        // 初始化后（并已进入首房）通知 UI 可交互
        Debug.Log("[RoomState] OnReady 触发（已进入首房）");
        OnReady?.Invoke();
    }

    /// <summary>
    /// 重置到初始状态（用于重新开始游戏）
    /// </summary>
    public void ResetToInitialState()
    {
        Debug.Log("[RoomState] 重置状态到初始状态");

        // 清空所有状态
        CurrentMap = null;
        CurrentRoom = null;
        visitedRooms.Clear();
        visitedOrderDebug.Clear();
        branchChoices.Clear();
        awaitingChoice = false;
        floorInitCounter = 0;

        // 清空敌人池
        EnemyPool.ClearDefeatedEnemies();

        Debug.Log("[RoomState] 状态重置完成");
    }

    /// <summary>
    /// 重置战斗房间的敌人池
    /// </summary>
    private void ResetCombatRoomEnemyPool()
    {
        // 清除已击败敌人记录
        EnemyPool.ClearDefeatedEnemies();
        Debug.Log("[RoomState] 已清除 EnemyPool 已击败敌人记录");
    }

    private void EnsureAtLeastOnePropsRoom(FloorMap_cza map, SeedRNG_cza rng)
    {
        if (map == null || map.Rooms == null || map.Rooms.Count == 0) return;
        bool hasProps = false;
        foreach (var kv in map.Rooms)
        {
            if (kv.Value != null && kv.Value.Type == RoomType_cza.Props)
            {
                hasProps = true;
                break;
            }
        }
        if (hasProps) return;

        var candidates = new List<int>();
        foreach (var kv in map.Rooms)
        {
            var id = kv.Key;
            var node = kv.Value;
            if (node == null) continue;
            if (id == 1) continue; // 避免将起始房改为 Props
            if (node.Type == RoomType_cza.Boss) continue;
            candidates.Add(id);
        }
        if (candidates.Count == 0) return;
        int pickIndex = rng != null ? rng.NextInt(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
        int pickId = candidates[pickIndex];
        var beforeType = map.Rooms[pickId].Type;
        map.Rooms[pickId].Type = RoomType_cza.Props;
        Debug.Log($"[RoomState] 保底 Props: Floor={map.FloorIndex} 将房间 {pickId} 类型 {beforeType} 替换为 Props");
    }

    // 进入指定房间
    public void EnterRoom(int roomId)
    {
        if (CurrentMap == null)
        {
            Debug.LogWarning("[RoomState] EnterRoom 失败: CurrentMap=null, roomId=" + roomId);
            return;
        }
        if (!CurrentMap.Rooms.ContainsKey(roomId))
        {
            Debug.LogWarning($"[RoomState] EnterRoom 失败: 房间 {roomId} 不存在 (RoomsCount={CurrentMap.Rooms.Count})");
            return;
        }
        if (visitedRooms.Contains(roomId))
        {
            Debug.LogWarning($"[RoomState] EnterRoom 拒绝: 房间 {roomId} 已访问，禁止重复进入 (visited=[{string.Join(",", visitedRooms)}])");
            return;
        }
        Debug.Log($"[RoomState] EnterRoom 准备进入: roomId={roomId} (visitedCount={visitedRooms.Count})");
        CurrentRoom = CurrentMap.Rooms[roomId];
        // 每次进入房间都退出选择模式，避免误触
        awaitingChoice = false;
        visitedRooms.Add(roomId);
        visitedOrderDebug.Add(roomId);
        Debug.Log($"[RoomState] 记录访问房间 -> {roomId}; 总数={visitedRooms.Count}; 集合=[{string.Join(",", visitedRooms)}]; 顺序=[{string.Join("->", visitedOrderDebug)}]; floorInitCounter={floorInitCounter}");
        branchChoices.Clear();
        Debug.Log($"[RoomState] 进入房间 Id={roomId} Type={CurrentRoom.Type}");
        var beforeInvokeRoom = CurrentRoom;
        HandleRoomEnter(CurrentRoom);
        if (OnRoomEntered != null)
        {
            foreach (var d in OnRoomEntered.GetInvocationList())
            {
                Debug.Log($"[RoomState] OnRoomEntered 调用订阅者: {d.Method.DeclaringType.FullName}.{d.Method.Name}() InstanceID={GetInstanceID()} CurrentRoom={(CurrentRoom!=null ? CurrentRoom.Id.ToString() : "null")}");
                d.DynamicInvoke(CurrentRoom);
                if (CurrentRoom == null)
                {
                    Debug.LogError("[RoomState] 警告: 某订阅者执行后 CurrentRoom 被设置为 null，尝试恢复");
                    if (beforeInvokeRoom != null)
                    {
                        CurrentRoom = beforeInvokeRoom;
                        Debug.LogError($"[RoomState] 恢复 CurrentRoom -> {CurrentRoom.Id}");
                    }
                }
            }
        }
        var afterInvokeRoom = CurrentRoom;
        if (beforeInvokeRoom != afterInvokeRoom)
        {
            Debug.LogWarning($"[RoomState] EnterRoom: CurrentRoom 在事件调用期间变化: before={(beforeInvokeRoom!=null?beforeInvokeRoom.Id.ToString():"null")} after={(afterInvokeRoom!=null?afterInvokeRoom.Id.ToString():"null")}");
        }
        OnBranchChoicesUpdated?.Invoke(branchChoices);
    }

    // 当前房间完成（战斗胜利/事件结束等）
    public void CompleteCurrentRoom()
    {
        // 防重复：若已在选择阶段，忽略再次完成触发，避免重复生成不同候选
        if (awaitingChoice)
        {
            Debug.Log("[RoomState] 已处于选择阶段，忽略重复 Complete 调用");
            return;
        }
        // 若尚未初始化楼层或未进入任何房间，此时不再兜底重置楼层，直接提示并返回，避免清空已访问记录
        if (CurrentMap == null || CurrentRoom == null)
        {
            string reason = (CurrentMap == null ? "CurrentMap=null" : "") + (CurrentRoom == null ? (CurrentMap == null ? ", " : "") + "CurrentRoom=null" : "");
            Debug.LogWarning($"[RoomState] Complete 调用时状态不完整 ({reason})，已取消本次完成操作，避免重置楼层");
            return;
        }
        Debug.Log($"[RoomState] 完成房间 Id={CurrentRoom.Id} Type={CurrentRoom.Type}");
        OnRoomCompleted?.Invoke(CurrentRoom);

        // 完成后进入选择模式（若有后续房间）
        // Boss 房直接结束
        if (CurrentRoom.Type == RoomType_cza.Boss)
        {
            awaitingChoice = false;
            int currentFloor = CurrentMap != null ? CurrentMap.FloorIndex : 0;
            int nextFloor = currentFloor + 1;
            Debug.Log($"[RoomState] Boss 房完成，自动初始化下一层 -> Floor {nextFloor}");
            InitFloor(nextFloor);
            return;
        }

        // 从“所有未访问的房间”中随机抽取最多3个
        branchChoices.Clear();
        var candidates = GetUnvisitedRoomIds();

        // 安全：理论上当前房间已在 visited 中，但这里确保不包含
        candidates.Remove(CurrentRoom.Id);

        if (candidates.Count == 0)
        {
            awaitingChoice = false;
            Debug.Log("[RoomState] 无后续分支（不存在未访问房间）");
            OnBranchChoicesUpdated?.Invoke(branchChoices);
            return;
        }

        // 采样不放回，最多3个
        var rng = SeedManager_cza.Instance != null ? SeedManager_cza.Instance.RNG : null;
        int need = Mathf.Min(3, candidates.Count);
        for (int i = 0; i < need; i++)
        {
            int pickIndex;
            if (rng != null)
            {
                pickIndex = rng.NextInt(0, candidates.Count);
            }
            else
            {
                pickIndex = UnityEngine.Random.Range(0, candidates.Count);
            }
            int pick = candidates[pickIndex];
            branchChoices.Add(pick);
            candidates.RemoveAt(pickIndex);
        }

        // 终态再次过滤：严禁包含任何已访问房间或当前房间（双保险）
        if (branchChoices.Count > 0)
        {
            // 去除已访问
            for (int i = branchChoices.Count - 1; i >= 0; i--)
            {
                int id = branchChoices[i];
                if (id == CurrentRoom.Id || visitedRooms.Contains(id))
                {
                    branchChoices.RemoveAt(i);
                }
            }
            // 去重
            var uniq = new HashSet<int>();
            for (int i = branchChoices.Count - 1; i >= 0; i--)
            {
                if (!uniq.Add(branchChoices[i])) branchChoices.RemoveAt(i);
            }
        }

        awaitingChoice = branchChoices.Count > 0;
        if (awaitingChoice)
        {
            // 打印已访问集合便于核对
            Debug.Log($"[RoomState] 已访问集合(Count={visitedRooms.Count})=[{string.Join(",", visitedRooms)}]; 顺序=[{string.Join("->", visitedOrderDebug)}]; floorInitCounter={floorInitCounter}");
            Debug.Log($"[RoomState] 选择阶段 可选={string.Join(",", branchChoices)}");
            for (int i = 0; i < branchChoices.Count; i++)
            {
                Debug.Log($"[RoomState] 选项 {i+1} -> 房间 {branchChoices[i]}");
            }
        }
        else
        {
            Debug.Log("[RoomState] 无后续分支");
        }
        OnBranchChoicesUpdated?.Invoke(new List<int>(branchChoices));
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
        if (!awaitingChoice) return;
        if (branchChoices.Count == 0) return;
        int raw = idx;
        idx = Mathf.Clamp(idx, 0, branchChoices.Count - 1);
        int target = branchChoices[idx];
        awaitingChoice = false;
        Debug.Log($"[RoomState] 选择输入={raw} 实际={idx+1} -> 进入房间 Id={target}");
        EnterRoom(target);
    }

    // 走向分支中的下一个房间
    public void GoToNext(int choiceIndex)
    {
        // 仅在选择阶段允许点击 Next
        if (!awaitingChoice) return;
        ChooseNextByIndex(choiceIndex);
    }

    // 根据房间类型执行进入时处理（可在此扩展生成怪物/刷新 UI 等）
    private void HandleRoomEnter(RoomNode_cza room)
    {
        Debug.Log($"[RoomState] HandleRoomEnter: 房间类型={room.Type}");
        
        switch (room.Type)
        {
            case RoomType_cza.Combat:
            {
                Debug.Log("[RoomState] 正在查找 CombatRoom_cza 组件...");
                var combat = UnityEngine.Object.FindObjectOfType<DreamWeavers.Rooms.CombatRoom_cza>();
                
                if (combat == null)
                {
                    Debug.Log("[RoomState] FindObjectOfType 未找到激活的 CombatRoom_cza，尝试查找未激活的...");
                    // 兼容未激活对象
                    var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.CombatRoom_cza>();
                    Debug.Log($"[RoomState] FindObjectsOfTypeAll 找到 {(all != null ? all.Length : 0)} 个 CombatRoom_cza");
                    
                    if (all != null && all.Length > 0)
                    {
                        combat = all[0];
                        Debug.Log($"[RoomState] 使用第一个 CombatRoom_cza: {combat.gameObject.name}, active={combat.gameObject.activeInHierarchy}");
                        // 不在这里激活 GameObject，由 RoomUI.SwitchToRoomTypePanel 控制面板显示
                    }
                }
                else
                {
                    Debug.Log($"[RoomState] FindObjectOfType 找到激活的 CombatRoom_cza: {combat.gameObject.name}");
                }
                
                if (combat != null)
                {
                    Debug.Log($"[RoomState] Enter CombatRoom -> calling EnterRoom() on {combat.gameObject.name}");
                    combat.EnterRoom();
                }
                else
                {
                    Debug.LogError("[RoomState] CombatRoom_cza not found! 请确保场景中有挂载 CombatRoom_cza 组件的 GameObject");
                }
                break;
            }
            case RoomType_cza.Rest:
            {
                var rest = UnityEngine.Object.FindObjectOfType<DreamWeavers.Rooms.RestRoom_cza>();
                if (rest == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.RestRoom_cza>();
                    if (all != null && all.Length > 0)
                    {
                        rest = all[0];
                        // 不在这里激活 GameObject，由 RoomUI.SwitchToRoomTypePanel 控制面板显示
                    }
                }
                if (rest != null)
                {
                    Debug.Log("[RoomState] Enter RestRoom -> calling EnterRoom()");
                    rest.EnterRoom();
                }
                else
                {
                    Debug.LogWarning("[RoomState] RestRoom_cza not found (active or inactive)");
                }
                break;
            }
            case RoomType_cza.Props:
            {
                var props = UnityEngine.Object.FindObjectOfType<DreamWeavers.Rooms.PropsRoom_cza>();
                if (props == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.PropsRoom_cza>();
                    if (all != null && all.Length > 0)
                    {
                        props = all[0];
                        // 不在这里激活 GameObject，由 RoomUI.SwitchToRoomTypePanel 控制面板显示
                    }
                }
                if (props != null)
                {
                    Debug.Log("[RoomState] Enter PropsRoom -> calling EnterRoom()");
                    props.EnterRoom();
                }
                else
                {
                    Debug.LogWarning("[RoomState] PropsRoom_cza not found (active or inactive)");
                }
                break;
            }
            case RoomType_cza.Events:
                // TODO: 事件房逻辑接入
                break;
            case RoomType_cza.Boss:
            {
                var boss = UnityEngine.Object.FindObjectOfType<DreamWeavers.Rooms.BossRoom_cza>();
                if (boss == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.BossRoom_cza>();
                    if (all != null && all.Length > 0)
                    {
                        boss = all[0];
                        // 不在这里激活 GameObject，由 RoomUI.SwitchToRoomTypePanel 控制面板显示
                    }
                }
                if (boss != null)
                {
                    Debug.Log("[RoomState] Enter BossRoom -> calling EnterRoom()");
                    boss.EnterRoom();
                }
                else
                {
                    Debug.LogWarning("[RoomState] BossRoom_cza not found (active or inactive)");
                }
                break;
            }
        }
    }
}
