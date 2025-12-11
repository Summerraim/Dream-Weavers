using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 对话条目
/// </summary>
[System.Serializable]
public class DialogueEntry
{
    public string speakerName;                    // 说话者名字
    [TextArea(3, 5)] public string dialogueText;  // 对话文本
    public Sprite portrait;                       // 角色立绘
    public UI_DialogView.PortraitPosition portraitPosition = UI_DialogView.PortraitPosition.Left; // 立绘位置
    public AudioClip voiceOver;                   // 语音
    public float displayTime = 0f;                // 显示时间（0表示使用默认）
    public bool isImportant = false;              // 是否为重要对话
    public bool waitForInput = true;              // 是否等待输入
    public string triggerEvent;                   // 触发事件名称
    public DialogueChoice[] choices;              // 对话选项
}

/// <summary>
/// 对话选项
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;                     // 选项文本
    public int nextDialogueIndex = -1;            // 跳转到的对话索引（-1表示结束对话）
    public string triggerEvent;                   // 触发事件名称
    public bool requireCondition;                 // 是否需要条件
    public string conditionName;                  // 条件名称
}

/// <summary>
/// 对话数据
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;                     // 对话ID
    public DialogueEntry[] dialogueEntries;       // 对话条目数组
    public bool canSkip = true;                   // 是否可以跳过
    public bool canAutoPlay = true;               // 是否可以自动播放
    public string onCompleteEvent;                // 对话完成时触发的事件
}

/// <summary>
/// 对话控制器
/// 负责管理对话流程、加载对话数据、处理对话事件
/// </summary>
public class DialogController : MonoBehaviour
{
    #region 单例实例
    
    private static DialogController _instance;
    public static DialogController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DialogController>();
                
                if (_instance == null)
                {
                    GameObject dialogController = new GameObject("DialogController");
                    _instance = dialogController.AddComponent<DialogController>();
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region 公共属性
    
    [Header("对话UI引用")]
    [SerializeField] private UI_DialogView dialogView;
    
    [Header("对话设置")]
    [SerializeField] private float defaultDisplayTime = 3f;   // 默认显示时间
    [SerializeField] private bool pauseGameDuringDialogue = true; // 对话时是否暂停游戏
    
    [Header("当前对话")]
    [SerializeField] private DialogueData currentDialogue;    // 当前对话数据
    [SerializeField] private int currentEntryIndex = -1;      // 当前对话条目索引
    
    #endregion
    
    #region 私有变量
    
    private bool isDialogueActive = false;        // 对话是否激活
    private bool isWaitingForChoice = false;      // 是否正在等待选择
    private Coroutine displayTimerCoroutine;      // 显示计时器协程
    
    // 对话历史记录
    private List<DialogueHistoryEntry> dialogueHistory = new List<DialogueHistoryEntry>();
    
    // 事件系统
    public event Action<DialogueData> OnDialogueStart;        // 对话开始事件
    public event Action<DialogueData> OnDialogueEnd;          // 对话结束事件
    public event Action<DialogueEntry> OnDialogueEntryShow;   // 对话条目显示事件
    public event Action<string> OnDialogueEventTriggered;     // 对话事件触发事件
    
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
    
    private void Start()
    {
        // 如果没有UI引用，尝试查找
        if (dialogView == null)
        {
            dialogView = FindObjectOfType<UI_DialogView>();
        }
        
        // 订阅UI事件
        if (dialogView != null)
        {
            dialogView.OnDialogContinue += OnContinueDialogue;
            dialogView.OnChoiceSelected += OnChoiceSelected;
            dialogView.OnDialogSkip += OnSkipDialogue;
            dialogView.OnDialogAutoToggle += OnAutoToggle;
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        if (dialogView != null)
        {
            dialogView.OnDialogContinue -= OnContinueDialogue;
            dialogView.OnChoiceSelected -= OnChoiceSelected;
            dialogView.OnDialogSkip -= OnSkipDialogue;
            dialogView.OnDialogAutoToggle -= OnAutoToggle;
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化对话控制器
    /// </summary>
    private void Initialize()
    {
        dialogueHistory.Clear();
        currentEntryIndex = -1;
        isDialogueActive = false;
        isWaitingForChoice = false;
    }
    
    #endregion
    
    #region 对话流程控制
    
    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || dialogueData.dialogueEntries.Length == 0)
        {
            Debug.LogError("对话数据为空或无效");
            return;
        }
        
        // 如果已经有对话在进行中，先结束它
        if (isDialogueActive)
        {
            EndDialogue();
        }
        
        // 设置当前对话
        currentDialogue = dialogueData;
        currentEntryIndex = -1;
        isDialogueActive = true;
        isWaitingForChoice = false;
        
        // 暂停游戏（如果需要）
        if (pauseGameDuringDialogue)
        {
            TryInvokeGameManagerMethod("PauseGame", fallbackPause: true);
        }
        
        // 显示对话UI
        if (dialogView != null)
        {
            dialogView.ShowDialogUI();
            dialogView.ClearDialogueText();
        }
        
        // 触发对话开始事件
        OnDialogueStart?.Invoke(dialogueData);
        
        // 显示第一个对话条目
        ShowNextDialogueEntry();
    }
    
    /// <summary>
    /// 继续对话
    /// </summary>
    private void OnContinueDialogue()
    {
        if (!isDialogueActive || isWaitingForChoice) return;
        
        ShowNextDialogueEntry();
    }
    
    /// <summary>
    /// 显示下一个对话条目
    /// </summary>
    private void ShowNextDialogueEntry()
    {
        if (currentDialogue == null) return;
        
        currentEntryIndex++;
        
        // 检查是否还有更多对话
        if (currentEntryIndex >= currentDialogue.dialogueEntries.Length)
        {
            EndDialogue();
            return;
        }
        
        // 获取当前对话条目
        DialogueEntry currentEntry = currentDialogue.dialogueEntries[currentEntryIndex];
        
        // 添加到历史记录
        AddToHistory(currentEntry);
        
        // 显示对话
        if (dialogView != null)
        {
            dialogView.ShowDialogue(
                currentEntry.speakerName,
                currentEntry.dialogueText,
                currentEntry.portrait,
                currentEntry.portraitPosition,
                currentEntry.isImportant
            );
            
            // 设置文本显示速度
            dialogView.SetTextSpeed(GetTextSpeed());
        }
        
        // 播放语音
        PlayVoiceOver(currentEntry.voiceOver);
        
        // 触发对话条目显示事件
        OnDialogueEntryShow?.Invoke(currentEntry);
        
        // 触发对话事件
        if (!string.IsNullOrEmpty(currentEntry.triggerEvent))
        {
            TriggerDialogueEvent(currentEntry.triggerEvent);
        }
        
        // 检查是否有选项
        if (currentEntry.choices != null && currentEntry.choices.Length > 0)
        {
            ShowChoices(currentEntry.choices);
            return;
        }
        
        // 如果不需要等待输入，设置自动继续
        if (!currentEntry.waitForInput)
        {
            float displayTime = currentEntry.displayTime > 0 ? currentEntry.displayTime : defaultDisplayTime;
            
            if (displayTimerCoroutine != null)
            {
                StopCoroutine(displayTimerCoroutine);
            }
            displayTimerCoroutine = StartCoroutine(AutoContinueDialogue(displayTime));
        }
    }
    
    /// <summary>
    /// 自动继续对话协程
    /// </summary>
    private IEnumerator AutoContinueDialogue(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowNextDialogueEntry();
    }
    
    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;
        
        // 恢复游戏（如果需要）
        if (pauseGameDuringDialogue)
        {
            TryInvokeGameManagerMethod("ResumeGame", fallbackPause: false);
        }
        
        // 隐藏对话UI
        if (dialogView != null)
        {
            dialogView.HideDialogUI();
        }
        
        // 触发对话完成事件
        if (!string.IsNullOrEmpty(currentDialogue.onCompleteEvent))
        {
            TriggerDialogueEvent(currentDialogue.onCompleteEvent);
        }
        
        // 触发对话结束事件
        OnDialogueEnd?.Invoke(currentDialogue);
        
        // 重置状态
        DialogueData completedDialogue = currentDialogue;
        currentDialogue = null;
        currentEntryIndex = -1;
        isDialogueActive = false;
        isWaitingForChoice = false;
        
        // 清理计时器
        if (displayTimerCoroutine != null)
        {
            StopCoroutine(displayTimerCoroutine);
            displayTimerCoroutine = null;
        }
        
        Debug.Log($"对话结束: {completedDialogue.dialogueId}");
    }
    
    #endregion
    
    #region 选项系统
    
    /// <summary>
    /// 显示选项
    /// </summary>
    private void ShowChoices(DialogueChoice[] choices)
    {
        if (dialogView == null || choices == null || choices.Length == 0) return;
        
        // 提取选项文本
        string[] choiceTexts = new string[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choiceTexts[i] = choices[i].choiceText;
            
            // 检查条件
            if (choices[i].requireCondition && !CheckCondition(choices[i].conditionName))
            {
                // 条件不满足，禁用选项
                choiceTexts[i] = $"[锁定] {choiceTexts[i]}";
            }
        }
        
        // 显示选项
        dialogView.ShowChoices(choiceTexts);
        isWaitingForChoice = true;
    }
    
    /// <summary>
    /// 选项选择事件处理
    /// </summary>
    private void OnChoiceSelected(int choiceIndex)
    {
        if (!isWaitingForChoice || currentDialogue == null) return;
        
        DialogueEntry currentEntry = currentDialogue.dialogueEntries[currentEntryIndex];
        if (currentEntry.choices == null || choiceIndex >= currentEntry.choices.Length) return;
        
        DialogueChoice selectedChoice = currentEntry.choices[choiceIndex];
        
        // 检查条件
        if (selectedChoice.requireCondition && !CheckCondition(selectedChoice.conditionName))
        {
            Debug.LogWarning($"条件不满足: {selectedChoice.conditionName}");
            return;
        }
        
        // 触发选项事件
        if (!string.IsNullOrEmpty(selectedChoice.triggerEvent))
        {
            TriggerDialogueEvent(selectedChoice.triggerEvent);
        }
        
        // 跳转到指定对话或结束对话
        if (selectedChoice.nextDialogueIndex >= 0 && selectedChoice.nextDialogueIndex < currentDialogue.dialogueEntries.Length)
        {
            currentEntryIndex = selectedChoice.nextDialogueIndex - 1; // -1因为ShowNextDialogueEntry会+1
            isWaitingForChoice = false;
            ShowNextDialogueEntry();
        }
        else
        {
            isWaitingForChoice = false;
            EndDialogue();
        }
    }
    
    /// <summary>
    /// 检查条件
    /// </summary>
    private bool CheckCondition(string conditionName)
    {
        // 这里需要根据你的游戏条件系统实现
        // 例如：检查任务状态、物品拥有情况、角色关系等
        // 暂时返回true
        return true;
    }
    
    #endregion
    
    #region 对话事件
    
    /// <summary>
    /// 触发对话事件
    /// </summary>
    private void TriggerDialogueEvent(string eventName)
    {
        Debug.Log($"触发对话事件: {eventName}");
        
        // 触发事件
        OnDialogueEventTriggered?.Invoke(eventName);
        
        // 这里可以根据事件名称执行不同的逻辑
        // 例如：触发游戏事件、改变游戏状态、播放动画等
        
        switch (eventName)
        {
            case "GiveItem":
                // 给予物品
                break;
                
            case "CompleteQuest":
                // 完成任务
                break;
                
            case "ChangeRelationship":
                // 改变关系
                break;
                
            case "PlayAnimation":
                // 播放动画
                break;
                
            case "ChangeScene":
                // 切换场景
                break;
        }
    }
    
    #endregion
    
    #region 语音播放
    
    /// <summary>
    /// 播放语音
    /// </summary>
    private void PlayVoiceOver(AudioClip voiceClip)
    {
        if (voiceClip != null && AudioManagerService.Instance != null)
        {
            AudioManagerService.Instance.PlaySFX(voiceClip);
        }
    }
    
    #endregion
    
    #region 跳过和自动播放
    
    /// <summary>
    /// 跳过对话
    /// </summary>
    private void OnSkipDialogue()
    {
        if (!isDialogueActive || !currentDialogue.canSkip) return;
        
        // 设置跳过状态
        if (dialogView != null)
        {
            dialogView.SetSkipState(true);
        }
        
        // 立即跳转到对话结尾
        EndDialogue();
    }
    
    /// <summary>
    /// 切换自动播放
    /// </summary>
    private void OnAutoToggle()
    {
        // UI已经处理了自动播放状态的切换
        // 这里可以添加额外的逻辑
    }
    
    #endregion
    
    #region 历史记录
    
    /// <summary>
    /// 添加到历史记录
    /// </summary>
    private void AddToHistory(DialogueEntry entry)
    {
        DialogueHistoryEntry historyEntry = new DialogueHistoryEntry
        {
            speakerName = entry.speakerName,
            dialogueText = entry.dialogueText,
            timestamp = DateTime.Now
        };
        
        dialogueHistory.Add(historyEntry);
        
        // 限制历史记录大小
        if (dialogueHistory.Count > 100)
        {
            dialogueHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 获取对话历史
    /// </summary>
    public List<DialogueHistoryEntry> GetDialogueHistory()
    {
        return new List<DialogueHistoryEntry>(dialogueHistory);
    }
    
    /// <summary>
    /// 清空对话历史
    /// </summary>
    public void ClearDialogueHistory()
    {
        dialogueHistory.Clear();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 检查对话是否激活
    /// </summary>
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    /// <summary>
    /// 获取当前对话数据
    /// </summary>
    public DialogueData GetCurrentDialogue()
    {
        return currentDialogue;
    }
    
    /// <summary>
    /// 从JSON加载对话数据
    /// </summary>
    public DialogueData LoadDialogueFromJSON(string jsonPath)
    {
        // 这里可以实现从JSON文件加载对话数据
        // 需要创建对应的JSON解析逻辑
        // 暂时返回null
        return null;
    }
    
    /// <summary>
    /// 从Resources加载对话数据
    /// </summary>
    public DialogueData LoadDialogueFromResources(string path)
    {
        DialogueData dialogueData = Resources.Load<DialogueData>(path);
        if (dialogueData == null)
        {
            Debug.LogError($"对话数据加载失败: {path}");
        }
        return dialogueData;
    }
    
    /// <summary>
    /// 快速开始对话（通过对话ID）
    /// </summary>
    public void QuickStartDialogue(string dialogueId)
    {
        // 从Resources加载对话数据
        DialogueData dialogueData = LoadDialogueFromResources($"Dialogues/{dialogueId}");
        if (dialogueData != null)
        {
            StartDialogue(dialogueData);
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 获取文本显示速度
    /// </summary>
    private float GetTextSpeed()
    {
        // 这里可以根据设置或玩家偏好调整
        return PlayerPrefs.GetFloat("DialogTextSpeed", 0.05f);
    }
    
    /// <summary>
    /// 设置文本显示速度
    /// </summary>
    public void SetTextSpeed(float speed)
    {
        PlayerPrefs.SetFloat("DialogTextSpeed", Mathf.Clamp(speed, 0.01f, 0.1f));
        PlayerPrefs.Save();
    }
    
    #endregion
    
    #region 辅助类
    
    /// <summary>
    /// 对话历史记录条目
    /// </summary>
    [System.Serializable]
    public class DialogueHistoryEntry
    {
        public string speakerName;
        public string dialogueText;
        public DateTime timestamp;
        
        public string GetFormattedEntry()
        {
            return $"[{timestamp:HH:mm}] {speakerName}: {dialogueText}";
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 通过反射尝试调用 GameManagerService 上的无参方法（PauseGame / ResumeGame）
    /// fallbackPause: 当方法不可用时，true 表示回退到设置 Time.timeScale = 0（暂停）；false 回退到 1（恢复）
    private void TryInvokeGameManagerMethod(string methodName, bool fallbackPause)
    {
        try
        {
            var gm = GameManagerService.Instance;
            if (gm != null)
            {
                var gmType = gm.GetType();
                var method = gmType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(gm, null);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"调用 GameManagerService.{methodName} 时发生异常: {ex.Message}");
        }
        
        // 回退行为：直接设置 Time.timeScale
        Time.timeScale = fallbackPause ? 0f : 1f;
    }
    
    #endregion
}