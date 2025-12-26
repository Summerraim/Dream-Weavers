using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 简化的对话控制器
/// 专注于基本的对话功能：显示对话者的头像和对话内容
/// </summary>
public class DialogController : MonoBehaviour
{
    [Header("对话UI引用")]
    [SerializeField] private UI_DialogView dialogView;
    
    [Header("对话设置")]
    [SerializeField] private DialogueData customDialogueData; // 自定义对话数据（可选）
    [SerializeField] private bool enableDebugLogs = true;     // 启用调试日志
    
    [Header("当前对话")]
    [SerializeField] private DialogueData currentDialogue;    // 当前对话数据
    [SerializeField] private int currentEntryIndex = -1;      // 当前对话条目索引
    
    // 事件
    public event Action OnDialogueEnd;            // 对话结束事件
    
    // 私有变量
    private bool isDialogueActive = false;        // 对话是否激活

    #region Unity生命周期

    private void Start()
    {
        LogDebug("DialogController Start方法开始执行");
        
        // 如果没有UI引用，尝试查找
        if (dialogView == null)
        {
            LogDebug("dialogView引用为空，尝试查找UI_DialogView...");
            dialogView = FindObjectOfType<UI_DialogView>();
            
            if (dialogView == null)
            {
                LogDebug("无法找到UI_DialogView组件！请确保UI_DialogView已添加到场景中。");
                LogDebug("对话系统需要UI_DialogView组件才能正常工作");
            }
            else
            {
                LogDebug("成功找到UI_DialogView组件");
            }
        }
        else
        {
            LogDebug("dialogView引用已设置");
        }
        
        // 订阅UI事件
        if (dialogView != null)
        {
            dialogView.OnDialogContinue += OnContinueDialogue;
            LogDebug("已订阅UI_DialogView的OnDialogContinue事件");
        }
        else
        {
            LogDebug("无法订阅UI事件，dialogView为空");
        }
        
        LogDebug("DialogController初始化完成");
    }

    private void OnDestroy()
    {
        // 清理事件订阅
        if (dialogView != null)
        {
            dialogView.OnDialogContinue -= OnContinueDialogue;
        }
    }

    private void Update()
    {
        // 按键触发功能已移除，对话现在通过房间进入自动触发
    }

    #endregion

    #region 协程方法

    /// <summary>
    /// 在结束当前对话后开始新对话
    /// </summary>
    private System.Collections.IEnumerator StartDialogueAfterEnd(DialogueData dialogueData)
    {
        // 等待一帧确保状态完全重置
        yield return null;
        
        // 重新开始对话
        StartDialogue(dialogueData);
    }

    #endregion

    #region 对话流程控制

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        LogDebug($"StartDialogue 被调用，对话数据: {(dialogueData != null ? dialogueData.dialogueId : "null")}");
        
        if (dialogueData == null || dialogueData.dialogueEntries.Length == 0)
        {
            LogDebug("对话数据为空或无效");
            return;
        }
        
        // 检查UI引用
        if (dialogView == null)
        {
            LogDebug("dialogView引用为空，尝试查找UI_DialogView...");
            dialogView = FindObjectOfType<UI_DialogView>();
            
            if (dialogView == null)
            {
                LogDebug("无法找到UI_DialogView组件！请确保UI_DialogView已添加到场景中。");
                return;
            }
            else
            {
                LogDebug("成功找到UI_DialogView组件");
                // 重新订阅事件
                dialogView.OnDialogContinue += OnContinueDialogue;
            }
        }
        
        // 检查UI组件是否完整
        if (!CheckUIComponents())
        {
            LogDebug("UI组件不完整，无法显示对话");
            return;
        }
        
        // 如果已经有对话在进行中，先结束它
        if (isDialogueActive)
        {
            LogDebug("结束当前对话，开始新对话");
            EndDialogue();
            // 使用协程延迟开始新对话，确保状态重置
            StartCoroutine(StartDialogueAfterEnd(dialogueData));
            return;
        }
        
        // 设置当前对话
        currentDialogue = dialogueData;
        currentEntryIndex = -1;
        isDialogueActive = true;
        
        // 显示对话UI
        dialogView.ShowDialogUI();
        dialogView.ClearDialogueText();
        
        // 显示第一个对话条目
        ShowNextDialogueEntry();
        
        LogDebug($"开始对话: {dialogueData.dialogueId}");
    }
    
    /// <summary>
    /// 检查UI组件是否完整
    /// </summary>
    private bool CheckUIComponents()
    {
        if (dialogView == null) return false;
        
        // 检查UI_DialogView组件是否已正确初始化
        bool hasDialogContainer = dialogView.transform.childCount > 0;
        
        // 尝试获取UI组件引用，但不强制要求所有组件都存在
        // 让UI_DialogView自己处理组件缺失的情况
        LogDebug($"UI组件检查 - 对话容器子对象数: {dialogView.transform.childCount}");
        
        return hasDialogContainer;
    }

    /// <summary>
    /// 继续对话
    /// </summary>
    private void OnContinueDialogue()
    {
        if (!isDialogueActive) return;
        
        ShowNextDialogueEntry();
    }

    /// <summary>
    /// 显示下一个对话条目
    /// </summary>
    private void ShowNextDialogueEntry()
    {
        LogDebug($"ShowNextDialogueEntry 被调用，当前索引: {currentEntryIndex}");
        
        if (currentDialogue == null)
        {
            LogDebug("当前对话数据为空");
            return;
        }
        
        currentEntryIndex++;
        
        // 检查是否还有更多对话
        if (currentEntryIndex >= currentDialogue.dialogueEntries.Length)
        {
            LogDebug("对话结束，没有更多条目");
            EndDialogue();
            return;
        }
        
        // 获取当前对话条目
        DialogueEntry currentEntry = currentDialogue.dialogueEntries[currentEntryIndex];
        
        LogDebug($"显示对话条目 {currentEntryIndex + 1}/{currentDialogue.dialogueEntries.Length}");
        LogDebug($"说话者: {currentEntry.speakerName}, 文本: {currentEntry.dialogueText}");
        
        // 确保dialogView存在
        if (dialogView == null)
        {
            LogDebug("dialogView为空，无法显示对话");
            return;
        }
        
        // 显示对话
        try
        {
            dialogView.ShowDialogue(
                currentEntry.speakerName ?? "未知",
                currentEntry.dialogueText ?? "暂无内容",
                currentEntry.portrait,
                currentEntry.portraitPosition
            );
            LogDebug("对话显示成功");
        }
        catch (Exception e)
        {
            LogDebug($"显示对话时发生错误: {e.Message}");
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;
        
        // 隐藏对话UI
        if (dialogView != null)
        {
            dialogView.HideDialogUI();
        }
        
        // 重置状态
        DialogueData completedDialogue = currentDialogue;
        currentDialogue = null;
        currentEntryIndex = -1;
        isDialogueActive = false;
        
        // 触发对话结束事件
        OnDialogueEnd?.Invoke();
        
        // 安全地记录对话结束
        if (completedDialogue != null)
        {
            LogDebug($"对话结束: {completedDialogue.dialogueId}");
        }
        else
        {
            LogDebug("对话结束");
        }
    }

    #endregion

    #region 调试工具

    /// <summary>
    /// 调试日志输出（根据enableDebugLogs设置）
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DialogController] {message}");
        }
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

    #endregion
}
