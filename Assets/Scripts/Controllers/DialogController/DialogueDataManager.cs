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
                speakerName = "男主",
                dialogueText = "这是什么地方？我怎么会出现在这里？我刚刚不是在家里打我的宝可梦吗？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "男主",
                dialogueText = "等等，前面这是什么动静？",
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
                speakerName = "心兽",
                dialogueText = "你好啊，欢迎来到心兽世界，你是我们的领导者，只有你能拯救我们的伙伴们。",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "玩家",
                dialogueText = "这是什么地方，为什么刚刚的心兽和你们有些不一样？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "心兽",
                dialogueText = "唉，我们的世界遭到了入侵，我们心兽原本代表了爱与和平，但现在却有不属于我们的邪恶力量在侵蚀我们的伙伴",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "心兽",
                dialogueText = "请带领我们战斗并拯救我们的伙伴吧！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "心兽",
                dialogueText = "这是休息的地方，在这里你可以恢复体力和精神，为接下来的冒险做好准备。",
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
                speakerName = "玩家",
                dialogueText = "这是什么呀？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "心兽",
                dialogueText = "哇！这些东西与我们体内的力量有所共鸣，他们可以激发我的潜能！",
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
                speakerName = "玩家",
                dialogueText = "这是什么地方？是我们第一次来吧？",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = false,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "心兽",
                dialogueText = "没错，这个地方很神秘，可能会发生一些意想不到的事情。",
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
                speakerName = "心兽",
                dialogueText = "哇，这就是我们的污染源了，他们是最纯粹的邪恶精神力量，比一般的心兽强大许多，要小心应对呀！",
                portraitPosition = UI_DialogView.PortraitPosition.Left,
                displayTime = 0f,
                isImportant = true,
                waitForInput = true,
                triggerEvent = "",
                choices = null
            },
            new DialogueEntry
            {
                speakerName = "玩家",
                dialogueText = "一起加油！",
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
                speakerName = "玩家",
                dialogueText = "这是什么地方？看起来像是一个森林，但是有些安静的太可怕了，往前走走看看吧",
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
                dialogueText = "你来到了森林，这里有很多奇怪的生物，它们会引起你的注意。",
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
                speakerName = "玩家",
                dialogueText = "唔~，好冷好冷，这是什么地方，怎么会如此寒冷？这里会遇到什么样的心兽呢？",
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
                dialogueText = "你来到了森林的另一边，也是寒武纪的开端，这里有很多奇怪的生物，它们会引起你的注意。",
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
                speakerName = "玩家",
                dialogueText = "终于离开寒武纪了，前面那是什么，好像是一座古堡，但是为什么这么诡异呢？",
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
                dialogueText = "冥王神话中哈迪斯创造了失乐园，而在此处，不知何人创造的失乐城堡",
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
                dialogueText = "好险！总算是从古堡中出来了。",
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
