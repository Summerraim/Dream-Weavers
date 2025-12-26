using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DreamWeavers.Services;

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
    private DialogControllerService dialogService;           // DialogControllerService引用
    
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
        
        // 获取DialogControllerService实例
        dialogService = DialogControllerService.Instance;
        if (dialogService != null)
        {
            Debug.Log("DialogControllerService 初始化成功");
        }
        else
        {
            Debug.LogWarning("DialogControllerService 初始化失败，对话功能可能无法正常工作");
        }
        
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
        Debug.Log($"RoomManager.EnterNewFloor 被调用，楼层: {floor}");
        
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
        Debug.Log("RoomManager: 准备触发关卡进入对话");
        TriggerFloorEnterDialogue();
        
        Debug.Log($"RoomManager: 进入新关卡完成: 楼层 {currentFloor}");
    }
    
    /// <summary>
    /// 触发关卡进入对话
    /// </summary>
    private void TriggerFloorEnterDialogue()
    {
        Debug.Log($"RoomManager.TriggerFloorEnterDialogue 被调用，enableFloorDialogue: {enableFloorDialogue}, hasTriggeredFloorDialogue: {hasTriggeredFloorDialogue}");
        
        if (!enableFloorDialogue || hasTriggeredFloorDialogue) 
        {
            Debug.Log("RoomManager: 跳过对话触发（已禁用或已触发）");
            return;
        }
        
        string dialogueId = $"Floor_{currentFloor}_Enter";
        Debug.Log($"RoomManager: 使用DialogControllerService加载对话数据: {dialogueId}");
        
        // 使用DialogControllerService获取对话数据
        DialogueData dialogueData = null;
        if (dialogService != null)
        {
            dialogueData = dialogService.GetDialogueData(dialogueId);
        }
        else
        {
            Debug.LogWarning("DialogControllerService未初始化，使用备用方法加载对话数据");
            dialogueData = LoadDialogueData(dialogueId);
        }
        
        if (dialogueData != null)
        {
            Debug.Log("RoomManager: 对话数据加载成功，查找DialogController...");
            DialogController dialogController = FindObjectOfType<DialogController>();
            if (dialogController != null)
            {
                Debug.Log($"RoomManager: 找到DialogController: {dialogController.gameObject.name}，开始对话");
                
                // 订阅对话结束事件
                dialogController.OnDialogueEnd += OnFloorDialogueEnd;
                
                dialogController.StartDialogue(dialogueData);
                hasTriggeredFloorDialogue = true;
                Debug.Log($"RoomManager: 触发关卡进入对话完成: {dialogueId}");
            }
            else
            {
                Debug.LogError("RoomManager: DialogController实例未找到！");
                Debug.LogError("请确保场景中有DialogController组件");
            }
        }
        else
        {
            Debug.LogWarning($"RoomManager: 未找到关卡进入对话数据: {dialogueId}");
        }
    }
    
    /// <summary>
    /// 关卡进入对话结束回调
    /// </summary>
    private void OnFloorDialogueEnd()
    {
        Debug.Log("RoomManager: 关卡进入对话结束，可以继续游戏流程");
        
        // 这里可以添加对话结束后的逻辑
        // 例如：显示房间选择界面、开始战斗等
        
        // 取消订阅事件
        DialogController dialogController = FindObjectOfType<DialogController>();
        if (dialogController != null)
        {
            dialogController.OnDialogueEnd -= OnFloorDialogueEnd;
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
        Debug.Log($"RoomManager: TriggerRoomEnterDialogue开始执行，房间类型: {roomType}, enableRoomDialogue: {enableRoomDialogue}");
        
        if (!enableRoomDialogue) 
        {
            Debug.Log("RoomManager: 房间对话已禁用，跳过触发");
            return;
        }
        
        Debug.Log($"RoomManager: 触发房间进入对话，房间类型: {roomType}");
        
        // 使用DialogControllerService获取对话数据
        DialogueData dialogueData = null;
        if (dialogService != null)
        {
            Debug.Log("RoomManager: 使用DialogControllerService获取对话数据");
            dialogueData = dialogService.GetDialogueForRoom(roomType);
        }
        else
        {
            Debug.LogWarning("RoomManager: DialogControllerService未初始化，使用备用方法加载对话数据");
            string dialogueId = GetRoomDialogueId(roomType);
            Debug.Log($"RoomManager: 备用方法 - 对话ID: {dialogueId}");
            dialogueData = LoadDialogueData(dialogueId);
        }
        
        if (dialogueData != null)
        {
            Debug.Log($"RoomManager: 对话数据加载成功: {dialogueData.dialogueId}");
            DialogController dialogController = FindObjectOfType<DialogController>();
            if (dialogController != null)
            {
                Debug.Log($"RoomManager: 找到DialogController: {dialogController.gameObject.name}，开始对话");
                dialogController.StartDialogue(dialogueData);
                Debug.Log($"RoomManager: 触发房间进入对话完成: {dialogueData.dialogueId}");
            }
            else
            {
                Debug.LogError("RoomManager: DialogController实例未找到！");
                Debug.LogError("RoomManager: 请确保场景中有DialogController组件");
            }
        }
        else
        {
            Debug.LogWarning($"RoomManager: 未找到房间类型 {roomType} 的对话数据");
        }
    }
    
    /// <summary>
    /// 加载对话数据
    /// </summary>
    private DialogueData LoadDialogueData(string dialogueId)
    {
        // 尝试从Resources加载对话数据
        DialogueData dialogueData = Resources.Load<DialogueData>($"Dialogues/{dialogueId}");
        
        if (dialogueData == null)
        {
            // 如果找不到对话数据，创建一个临时的对话数据
            dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.dialogueId = dialogueId;
            dialogueData.dialogueEntries = new DialogueEntry[]
            {
                new DialogueEntry
                {
                    speakerName = "系统",
                    dialogueText = $"这是{dialogueId}的临时对话内容。请创建对应的对话数据文件。",
                    portraitPosition = UI_DialogView.PortraitPosition.None
                }
            };
            Debug.LogWarning($"使用临时对话数据: {dialogueId}");
        }
        
        return dialogueData;
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
