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
    // 新增：楼层已生成但尚未进入首个房间（用于外部提前准备，例如切换CombatRoom的EnemyPool）
    public event Action<int> OnFloorPreparing;
    // 新增：当前楼层变化事件（参数：旧楼层, 新楼层）
    public event Action<int, int> OnCurrentFloorChanged;

    // 新增：是否处于“等待玩家选择分支”的状态
    private bool awaitingChoice;
    public bool IsAwaitingChoice => awaitingChoice;
    // 当前楼层索引（类级别变量，便于外部监听）
    private int currentFloor;
    public int CurrentFloor => currentFloor;
    // 下一楼层索引（基于当前楼层计算）
    public int NextFloor => currentFloor + 1;
    // 起始楼层（公开只读访问）
    public int StartFloor => startFloor;
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
    public void InitFloor(int floor, bool enterStartRoom = true)
    {
        var rng = SeedManager_cza.Instance != null ? SeedManager_cza.Instance.RNG : null;
        Debug.Log($"[RoomState] InitFloor({floor}) 调用，floorInitCounter={floorInitCounter} -> {floorInitCounter + 1}; SeedManager={(SeedManager_cza.Instance!=null)} RNG={(rng!=null)}");
        floorInitCounter++;
        
        // 更新当前楼层并触发事件
        int oldFloor = currentFloor;
        currentFloor = floor;
        if (oldFloor != currentFloor)
        {
            Debug.Log($"[RoomState] 楼层变化: {oldFloor} -> {currentFloor}");
            try { OnCurrentFloorChanged?.Invoke(oldFloor, currentFloor); } catch (Exception ex) { Debug.LogError($"[RoomState] OnCurrentFloorChanged 异常: {ex}"); }
        }
        Debug.Log($"<color=cyan>[FloorTracker] InitFloor 完成 - CurrentFloor={currentFloor}, NextFloor={NextFloor}, StartFloor={StartFloor}</color>");
        
        Debug.Log($"[RoomState] 清空访问集，之前数量={visitedRooms.Count} 内容=[{string.Join(",", visitedRooms)}]");
        visitedRooms.Clear();
        visitedOrderDebug.Clear();
        branchChoices.Clear();
        awaitingChoice = false;
        CurrentRoom = null;
        
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
        // 通知：楼层已生成，尚未进入首个房间（用于外部提前准备，例如切换CombatRoom的EnemyPool）
        try { OnFloorPreparing?.Invoke(floor); } catch (Exception ex) { Debug.LogError("[RoomState] OnFloorPreparing 异常: " + ex); }

        if (enterStartRoom)
        {
            EnterRoom(startId); // 进入起始房（记录为 visited）
            Debug.Log($"[RoomState] 初始化楼层 {floor} 完成，CurrentRoom={(CurrentRoom!=null ? CurrentRoom.Id.ToString() : "null")}");

            // 通知：楼层初始化完成（用于UI隐藏上一层残留）
            try { OnFloorInitialized?.Invoke(floor); } catch (Exception ex) { Debug.LogError($"[RoomState] OnFloorInitialized 异常: {ex}"); }
            // 初始化后（并已进入首房）通知 UI 可交互
            Debug.Log("[RoomState] OnReady 触发（已进入首房）");
            OnReady?.Invoke();
        }
        else
        {
            Debug.Log($"[RoomState] 初始化楼层 {floor} 完成（延迟进入起始房，等待玩家选择路线）");
            // 通知：楼层初始化完成（用于UI隐藏上一层残留）
            try { OnFloorInitialized?.Invoke(floor); } catch (Exception ex) { Debug.LogError($"[RoomState] OnFloorInitialized 异常: {ex}"); }
        }
    }

    /// <summary>
    /// 进入“路线选择”阶段（不依赖当前房间），从未访问房间中抽取最多3个作为候选。
    /// </summary>
    public void BeginRouteSelection(int maxChoices = 3, IReadOnlyCollection<int> excludeRoomIds = null)
    {
        if (CurrentMap == null || CurrentMap.Rooms == null || CurrentMap.Rooms.Count == 0)
        {
            Debug.LogWarning("[RoomState] BeginRouteSelection: CurrentMap 为空，无法进入路线选择");
            return;
        }

        branchChoices.Clear();

        var candidates = GetUnvisitedRoomIds();
        HashSet<int> excluded = null;
        if (excludeRoomIds != null && excludeRoomIds.Count > 0)
        {
            excluded = new HashSet<int>(excludeRoomIds);
        }

        if (excluded != null)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (excluded.Contains(candidates[i]))
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        if (candidates.Count == 0)
        {
            awaitingChoice = false;
            OnBranchChoicesUpdated?.Invoke(branchChoices);
            Debug.Log("[RoomState] BeginRouteSelection: 无候选房间可选");
            return;
        }

        int need = Mathf.Min(Mathf.Max(1, maxChoices), candidates.Count);
        var rng = SeedManager_cza.Instance != null ? SeedManager_cza.Instance.RNG : null;
        for (int i = 0; i < need; i++)
        {
            int pickIndex = rng != null ? rng.NextInt(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            int pick = candidates[pickIndex];
            branchChoices.Add(pick);
            candidates.RemoveAt(pickIndex);
        }

        awaitingChoice = branchChoices.Count > 0;
        if (awaitingChoice)
        {
            var details = new List<string>();
            foreach (var rid in branchChoices)
            {
                string t = CurrentMap.Rooms.TryGetValue(rid, out var n) ? n.Type.ToString() : "?";
                details.Add($"{rid}({t})");
            }
            Debug.Log($"[RoomState] BeginRouteSelection: 可选房间={string.Join(", ", details)}");
        }

        OnBranchChoicesUpdated?.Invoke(new List<int>(branchChoices));
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
        
        // 重置楼层（触发事件通知）
        int oldFloor = currentFloor;
        currentFloor = 0;
        if (oldFloor != currentFloor)
        {
            try { OnCurrentFloorChanged?.Invoke(oldFloor, currentFloor); } catch (Exception ex) { Debug.LogError($"[RoomState] OnCurrentFloorChanged 异常: {ex}"); }
        }

        // 清空敌人池
        EnemyPool.ClearDefeatedEnemies();

        Debug.Log($"<color=magenta>[FloorTracker] 状态重置完成 - CurrentFloor={currentFloor}, NextFloor={NextFloor}, StartFloor={StartFloor}</color>");
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
        if (CurrentMap == null || !CurrentMap.Rooms.ContainsKey(roomId))
            return;
        if (visitedRooms.Contains(roomId))
            return;
            
        CurrentRoom = CurrentMap.Rooms[roomId];
        awaitingChoice = false;
        visitedRooms.Add(roomId);
        visitedOrderDebug.Add(roomId);
        branchChoices.Clear();
        
        Debug.Log($"[Room] 进入房间 Id={roomId} Type={CurrentRoom.Type} (CurrentMap.FloorIndex={CurrentMap.FloorIndex})");
        Debug.Log($"<color=green>[FloorTracker] EnterRoom - CurrentFloor={currentFloor}, NextFloor={NextFloor}</color>");
        
        var beforeInvokeRoom = CurrentRoom;
        Debug.Log($"[Room] 即将调用 HandleRoomEnter, CurrentRoom.Type={CurrentRoom.Type}");
        HandleRoomEnter(CurrentRoom);
        Debug.Log($"[Room] HandleRoomEnter 完成, CurrentRoom.Type={CurrentRoom?.Type}");
        if (OnRoomEntered != null)
        {
            foreach (var d in OnRoomEntered.GetInvocationList())
            {
                d.DynamicInvoke(CurrentRoom);
                if (CurrentRoom == null && beforeInvokeRoom != null)
                {
                    CurrentRoom = beforeInvokeRoom;
                }
            }
        }
        OnBranchChoicesUpdated?.Invoke(branchChoices);
    }

    // 当前房间完成（战斗胜利/事件结束等）
    public void CompleteCurrentRoom()
    {
        // 防重复：若已在选择阶段，忽略再次完成触发
        if (awaitingChoice)
            return;
        if (CurrentMap == null || CurrentRoom == null)
            return;
            
        Debug.Log($"[Room] 完成房间 Id={CurrentRoom.Id} Type={CurrentRoom.Type}");
        OnRoomCompleted?.Invoke(CurrentRoom);

        // Boss 房直接进入下一层
        if (CurrentRoom.Type == RoomType_cza.Boss)
        {
            awaitingChoice = false;
            Debug.Log($"<color=yellow>[FloorTracker] Boss房完成! 当前CurrentFloor={currentFloor}, 即将进入NextFloor={NextFloor}</color>");
            // 使用类级别的 NextFloor 属性（基于 currentFloor 计算）
            InitFloor(NextFloor);
            return;
        }

        // 从“所有未访问的房间”中随机抽取最多3个
        branchChoices.Clear();
        var candidates = GetUnvisitedRoomIds();

        // 安全：确保不包含当前房间
        candidates.Remove(CurrentRoom.Id);
        
        // 调试：打印候选和已访问
        Debug.Log($"[Room] 候选(排除当前{CurrentRoom.Id}): [{string.Join(",", candidates)}] visited=[{string.Join(",", visitedRooms)}]");

        if (candidates.Count == 0)
        {
            awaitingChoice = false;
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
            // 关键日志：显示当前可选路线及其类型
            var details = new List<string>();
            foreach (var rid in branchChoices)
            {
                string t = CurrentMap.Rooms.TryGetValue(rid, out var n) ? n.Type.ToString() : "?";
                details.Add($"{rid}({t})");
            }
            Debug.Log($"[Room] 选择阶段: 可选房间={string.Join(", ", details)}");
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
        
        // 诊断日志：显示点击的按钮索引和当前 branchChoices
        var details = new List<string>();
        for (int i = 0; i < branchChoices.Count; i++)
        {
            int rid = branchChoices[i];
            string t = CurrentMap.Rooms.TryGetValue(rid, out var n) ? n.Type.ToString() : "?";
            details.Add($"[{i}]={rid}({t})");
        }
        Debug.Log($"[Room] ChooseNextByIndex: 点击按钮idx={idx}, branchChoices={string.Join(", ", details)}");
        
        idx = Mathf.Clamp(idx, 0, branchChoices.Count - 1);
        int target = branchChoices[idx];
        Debug.Log($"[Room] ChooseNextByIndex: 实际选择 idx={idx} -> roomId={target}");
        awaitingChoice = false;
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
            case RoomType_cza.Skill:
            {
                var skill = UnityEngine.Object.FindObjectOfType<DreamWeavers.Rooms.SkillRoom_cza>();
                if (skill == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<DreamWeavers.Rooms.SkillRoom_cza>();
                    Debug.Log($"[RoomState] 查找 SkillRoom_cza 组件，找到数量={(all != null ? all.Length : 0)}");
                    if (all != null && all.Length > 0)
                    {
                        skill = all[0];
                    }
                }

                if (skill != null)
                {
                    Debug.Log("[RoomState] Enter SkillRoom -> calling EnterRoom()");
                    skill.EnterRoom();
                }
                else
                {
                    Debug.LogWarning("[RoomState] SkillRoom_cza not found (active or inactive)");
                }
                break;
            }
            // case RoomType_cza.Events:
                // TODO: 事件房逻辑接入
                // break;
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
