using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话数据管理器
/// 通过代码管理对话数据，避免依赖Unity编辑器创建.asset文件
/// </summary>
public class DialogueDataManager : MonoBehaviour
{
    #region 单例实例
    
    private static DialogueDataManager _instance;
    public static DialogueDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DialogueDataManager>();
                
                if (_instance == null)
                {
                    GameObject manager = new GameObject("DialogueDataManager");
                    _instance = manager.AddComponent<DialogueDataManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region 对话数据存储
    
    private Dictionary<string, DialogueData> dialogueDataCache = new Dictionary<string, DialogueData>();
    
    #endregion
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化对话数据
            InitializeDialogueData();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 确保在场景加载后对话数据已经初始化
        if (_instance == this && dialogueDataCache.Count == 0)
        {
            InitializeDialogueData();
        }
    }
    
    #endregion
    
    #region 对话数据初始化
    
    /// <summary>
    /// 初始化对话数据
    /// </summary>
    private void InitializeDialogueData()
    {
        // 战斗房间进入对话
        CreateCombatRoomDialogue();
        
        // 休息房间进入对话
        CreateRestRoomDialogue();
        
        // 道具房间进入对话
        CreatePropsRoomDialogue();
        
        // 事件房间进入对话
        CreateEventsRoomDialogue();
        
        // Boss房间进入对话
        CreateBossRoomDialogue();
        
        // 关卡进入对话
        CreateFloorEnterDialogues();
        
        Debug.Log("对话数据管理器初始化完成");
    }
    
    /// <summary>
    /// 创建战斗房间进入对话
    /// </summary>
    private void CreateCombatRoomDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Room_Combat_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        // 对话条目
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "男主",
                dialogueText = "这是什么地方？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "啊啊啊这是什么东西，难道是恐龙吗？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "敌人已经出现在你面前，小心应对！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Room_Combat_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建休息房间进入对话
    /// </summary>
    private void CreateRestRoomDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Room_Rest_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "你发现了一个安全的休息场所。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "在这里你可以恢复体力和精神，为接下来的冒险做好准备。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Room_Rest_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建道具房间进入对话
    /// </summary>
    private void CreatePropsRoomDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Room_Props_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "你发现了一个道具房间！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "这里可能有对你有用的物品，仔细搜索一下吧。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Room_Props_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建事件房间进入对话
    /// </summary>
    private void CreateEventsRoomDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Room_Events_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "你进入了一个神秘的事件房间。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "这里可能会发生意想不到的事情，保持警惕！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Room_Events_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建Boss房间进入对话
    /// </summary>
    private void CreateBossRoomDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Room_Boss_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "警告！你进入了Boss房间！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = true,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "强大的敌人正在等待着你，这将是一场艰苦的战斗！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = true,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Room_Boss_Enter"] = dialogueData;
    }
    
    #endregion
    
    #region 关卡进入对话
    
    /// <summary>
    /// 创建关卡进入对话
    /// </summary>
    private void CreateFloorEnterDialogues()
    {
        // 创建第1层进入对话
        CreateFloor1EnterDialogue();
        
        // 创建第2层进入对话
        CreateFloor2EnterDialogue();
        
        // 创建第3层进入对话
        CreateFloor3EnterDialogue();
        
        // 创建第4层进入对话
        CreateFloor4EnterDialogue();
    }
    
    /// <summary>
    /// 创建第1层进入对话
    /// </summary>
    private void CreateFloor1EnterDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Floor_1_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "欢迎来到第1层！这是你的冒险开始的地方。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "小心探索每个房间，收集资源，为接下来的挑战做好准备。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Floor_1_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建第2层进入对话
    /// </summary>
    private void CreateFloor2EnterDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Floor_2_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "恭喜你成功通过第1层！现在你来到了第2层。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "这一层的敌人会更加危险，但奖励也会更加丰厚。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Floor_2_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建第3层进入对话
    /// </summary>
    private void CreateFloor3EnterDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Floor_3_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "你已经到达第3层！这里的挑战将更加严峻。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "Boss房间就在前方，确保你已经做好了充分的准备。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Floor_3_Enter"] = dialogueData;
    }
    
    /// <summary>
    /// 创建第4层进入对话
    /// </summary>
    private void CreateFloor4EnterDialogue()
    {
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.dialogueId = "Floor_4_Enter";
        dialogueData.canSkip = true;
        dialogueData.canAutoPlay = true;
        
        dialogueData.dialogueEntries = new DialogueEntry[]
        {
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "最终层！第4层！这是你冒险的终点。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = true,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "系统",
                dialogueText = "最终的Boss正在等待着你，这将是最艰难的战斗！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = true,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            }
        };
        
        dialogueDataCache["Floor_4_Enter"] = dialogueData;
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 获取对话数据
    /// </summary>
    public DialogueData GetDialogueData(string dialogueId)
    {
        if (dialogueDataCache.ContainsKey(dialogueId))
        {
            return dialogueDataCache[dialogueId];
        }
        
        Debug.LogWarning($"未找到对话数据: {dialogueId}");
        return null;
    }
    
    /// <summary>
    /// 添加自定义对话数据
    /// </summary>
    public void AddDialogueData(string dialogueId, DialogueData dialogueData)
    {
        if (dialogueDataCache.ContainsKey(dialogueId))
        {
            Debug.LogWarning($"对话数据已存在: {dialogueId}，将被覆盖");
        }
        
        dialogueDataCache[dialogueId] = dialogueData;
        Debug.Log($"已添加对话数据: {dialogueId}");
    }
    
    /// <summary>
    /// 检查对话数据是否存在
    /// </summary>
    public bool HasDialogueData(string dialogueId)
    {
        return dialogueDataCache.ContainsKey(dialogueId);
    }
    
    #endregion
}
