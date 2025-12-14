using UnityEngine;

/// <summary>
/// 房间系统测试脚本
/// 用于演示房间切换和对话触发功能
/// </summary>
public class RoomSystemTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;
    
    private void Update()
    {
        if (!enableTesting) return;
        
        // 按T键测试房间系统
        if (Input.GetKeyDown(testKey))
        {
            TestRoomSystem();
        }
    }
    
    /// <summary>
    /// 测试房间系统
    /// </summary>
    private void TestRoomSystem()
    {
        Debug.Log("=== 房间系统测试开始 ===");
        
        // 获取房间管理器实例
        RoomManager roomManager = RoomManager.Instance;
        
        if (roomManager == null)
        {
            Debug.LogError("RoomManager实例未找到！");
            return;
        }
        
        // 测试进入第1层
        Debug.Log("测试：进入第1层");
        roomManager.EnterNewFloor(1);
        
        // 测试进入房间
        Debug.Log("测试：进入房间1（战斗房间）");
        roomManager.EnterRoom(1);
        
        // 测试进入房间2（假设是Boss房间）
        Debug.Log("测试：进入房间7（Boss房间）");
        roomManager.EnterRoom(7);
        
        Debug.Log("=== 房间系统测试完成 ===");
        Debug.Log("注意：实际游戏中，房间切换应该通过UI或游戏逻辑触发，而不是直接调用这些方法。");
    }
    
    /// <summary>
    /// 快速测试关卡进入对话
    /// </summary>
    public void QuickTestFloorDialogue(int floor)
    {
        if (floor < 1 || floor > 4)
        {
            Debug.LogError($"无效的楼层: {floor}");
            return;
        }
        
        string dialogueId = $"Floor_{floor}_Enter";
        DialogueData dialogueData = DialogueDataManager.Instance.GetDialogueData(dialogueId);
        
        if (dialogueData != null)
        {
            DialogController.Instance.StartDialogue(dialogueData);
            Debug.Log($"触发关卡进入对话: {dialogueId}");
        }
        else
        {
            Debug.LogWarning($"未找到关卡进入对话数据: {dialogueId}");
        }
    }
    
    /// <summary>
    /// 快速测试房间进入对话
    /// </summary>
    public void QuickTestRoomDialogue(RoomType_cza roomType)
    {
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
}
