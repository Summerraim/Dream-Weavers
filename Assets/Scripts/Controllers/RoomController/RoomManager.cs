using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 房间管理器
/// 负责管理房间切换和对话触发
/// </summary>
public class RoomManager : MonoBehaviour
{
    #region 单例实例
    
    private static RoomManager _instance;
    public static RoomManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RoomManager>();
                
                if (_instance == null)
                {
                    GameObject roomManager = new GameObject("RoomManager");
                    _instance = roomManager.AddComponent<RoomManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region 公共属性
    
    [Header("当前状态")]
    [SerializeField] private int currentFloor = 1;           // 当前楼层
    [SerializeField] private int currentRoomId = 1;          // 当前房间ID
    [SerializeField] private RoomType_cza currentRoomType;   // 当前房间类型
    
    [Header("对话设置")]
    [SerializeField] private bool enableFloorDialogue = true;    // 是否启用关卡进入对话
    [SerializeField] private bool enableRoomDialogue = true;     // 是否启用房间进入对话
    
    #endregion
    
    #region 私有变量
    
    private FloorMap_cza currentFloorMap;                    // 当前楼层地图
    private bool hasTriggeredFloorDialogue = false;          // 是否已触发当前楼层对话
    
    #endregion
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化
        Initialize();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化房间管理器
    /// </summary>
    private void Initialize()
    {
        currentFloor = 1;
        currentRoomId = 1;
        hasTriggeredFloorDialogue = false;
        
        // 生成初始楼层地图
        GenerateFloorMap(currentFloor);
        
        Debug.Log($"房间管理器初始化完成 - 楼层: {currentFloor}, 房间: {currentRoomId}");
    }
    
    #endregion
    
    #region 楼层管理
    
    /// <summary>
    /// 生成楼层地图
    /// </summary>
    private void GenerateFloorMap(int floor)
    {
        // 使用地图生成器生成楼层地图
        SeedRNG_cza rng = new SeedRNG_cza(System.DateTime.Now.Millisecond);
        currentFloorMap = MapGenerator_cza.GenerateFloor(floor, rng);
        
        Debug.Log($"生成楼层 {floor} 地图完成，共 {currentFloorMap.Rooms.Count} 个房间");
    }
    
    /// <summary>
    /// 进入新关卡（楼层）
    /// </summary>
    public void EnterNewFloor(int floor)
    {
        if (floor < 1 || floor > 4)
        {
            Debug.LogError($"无效的楼层: {floor}");
            return;
        }
        
        currentFloor = floor;
        currentRoomId = 1;
        hasTriggeredFloorDialogue = false;
        
        // 生成新的楼层地图
        GenerateFloorMap(currentFloor);
        
        // 触发关卡进入对话
        TriggerFloorEnterDialogue();
        
        Debug.Log($"进入新关卡: 楼层 {currentFloor}");
    }
    
    /// <summary>
    /// 触发关卡进入对话
    /// </summary>
    private void TriggerFloorEnterDialogue()
    {
        if (!enableFloorDialogue || hasTriggeredFloorDialogue) return;
        
        string dialogueId = $"Floor_{currentFloor}_Enter";
        DialogueData dialogueData = DialogueDataManager.Instance.GetDialogueData(dialogueId);
        
        if (dialogueData != null)
        {
            DialogController.Instance.StartDialogue(dialogueData);
            hasTriggeredFloorDialogue = true;
            Debug.Log($"触发关卡进入对话: {dialogueId}");
        }
        else
        {
            Debug.LogWarning($"未找到关卡进入对话数据: {dialogueId}");
        }
    }
    
    #endregion
    
    #region 房间管理
    
    /// <summary>
    /// 进入房间
    /// </summary>
    public void EnterRoom(int roomId)
    {
        if (currentFloorMap == null || !currentFloorMap.Rooms.ContainsKey(roomId))
        {
            Debug.LogError($"房间不存在: 楼层 {currentFloor}, 房间 {roomId}");
            return;
        }
        
        RoomNode_cza roomNode = currentFloorMap.Rooms[roomId];
        currentRoomId = roomId;
        currentRoomType = roomNode.Type;
        
        // 触发房间进入对话
        TriggerRoomEnterDialogue(roomNode.Type);
        
        Debug.Log($"进入房间: 楼层 {currentFloor}, 房间 {roomId}, 类型: {roomNode.Type}");
    }
    
    /// <summary>
    /// 触发房间进入对话
    /// </summary>
    private void TriggerRoomEnterDialogue(RoomType_cza roomType)
    {
        if (!enableRoomDialogue) return;
        
        string dialogueId = GetRoomDialogueId(roomType);
        DialogueData dialogueData = DialogueDataManager.Instance.GetDialogueData(dialogueId);
        
        if (dialogueData != null)
        {
            DialogController.Instance.StartDialogue(dialogueData);
            Debug.Log($"触发房间进入对话: {dialogueId}");
        }
        else
        {
            Debug.LogWarning($"未找到房间进入对话数据: {dialogueId}");
        }
    }
    
    /// <summary>
    /// 根据房间类型获取对话ID
    /// </summary>
    private string GetRoomDialogueId(RoomType_cza roomType)
    {
        switch (roomType)
        {
            case RoomType_cza.Combat:
                return "Room_Combat_Enter";
            case RoomType_cza.Rest:
                return "Room_Rest_Enter";
            case RoomType_cza.Props:
                return "Room_Props_Enter";
            case RoomType_cza.Events:
                return "Room_Events_Enter";
            case RoomType_cza.Boss:
                return "Room_Boss_Enter";
            default:
                return "Room_Combat_Enter";
        }
    }
    
    /// <summary>
    /// 获取下一个可进入的房间
    /// </summary>
    public List<int> GetNextRooms()
    {
        if (currentFloorMap == null || !currentFloorMap.Rooms.ContainsKey(currentRoomId))
        {
            return new List<int>();
        }
        
        RoomNode_cza currentRoom = currentFloorMap.Rooms[currentRoomId];
        return new List<int>(currentRoom.NextRooms);
    }
    
    /// <summary>
    /// 移动到下一个房间
    /// </summary>
    public void MoveToNextRoom(int nextRoomId)
    {
        if (currentFloorMap == null || !currentFloorMap.Rooms.ContainsKey(nextRoomId))
        {
            Debug.LogError($"下一个房间不存在: {nextRoomId}");
            return;
        }
        
        // 离开当前房间
        ExitCurrentRoom();
        
        // 进入下一个房间
        EnterRoom(nextRoomId);
    }
    
    /// <summary>
    /// 离开当前房间
    /// </summary>
    private void ExitCurrentRoom()
    {
        // 这里可以添加离开房间的逻辑
        // 例如：保存房间状态、清理资源等
        Debug.Log($"离开房间: 楼层 {currentFloor}, 房间 {currentRoomId}");
    }
    
    /// <summary>
    /// 检查是否到达Boss房间
    /// </summary>
    public bool IsBossRoom()
    {
        return currentRoomType == RoomType_cza.Boss;
    }
    
    /// <summary>
    /// 检查是否完成当前楼层
    /// </summary>
    public bool IsFloorCompleted()
    {
        if (currentFloorMap == null) return false;
        
        // 如果当前房间是最后一个房间（Boss房间）且已完成，则认为楼层完成
        int lastRoomId = currentFloorMap.Rooms.Count;
        return currentRoomId == lastRoomId && IsBossRoom();
    }
    
    /// <summary>
    /// 进入下一个楼层
    /// </summary>
    public void EnterNextFloor()
    {
        if (currentFloor >= 4)
        {
            Debug.Log("游戏完成！已到达最后一个楼层");
            // 触发游戏完成事件
            return;
        }
        
        EnterNewFloor(currentFloor + 1);
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 获取当前楼层
    /// </summary>
    public int GetCurrentFloor()
    {
        return currentFloor;
    }
    
    /// <summary>
    /// 获取当前房间ID
    /// </summary>
    public int GetCurrentRoomId()
    {
        return currentRoomId;
    }
    
    /// <summary>
    /// 获取当前房间类型
    /// </summary>
    public RoomType_cza GetCurrentRoomType()
    {
        return currentRoomType;
    }
    
    /// <summary>
    /// 获取当前楼层地图
    /// </summary>
    public FloorMap_cza GetCurrentFloorMap()
    {
        return currentFloorMap;
    }
    
    #endregion
}
