using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 事件中心服务 - 使用发布-订阅模式
public class EventCenterService : Singleton<EventCenterService>
{
    // 事件字典：事件名 -> 回调列表
    private Dictionary<string, List<Delegate>> eventDictionary =
        new Dictionary<string, List<Delegate>>();

    // 延迟事件队列（避免在事件处理中触发新事件导致的循环）
    private Queue<DelayedEvent> delayedEvents = new Queue<DelayedEvent>();
    private bool isProcessingEvents = false;

    [Header("Event Center Settings")]
    public bool enableDebug = false;
    public int maxEventsPerFrame = 100;

    // 初始化
    public override void Initialize()
    {
        if (enableDebug)
            Debug.Log("EventCenterService initialized");
    }

    #region 基础事件注册和触发

    // 添加事件监听（无参数）
    public void AddListener(string eventName, Action callback)
    {
        AddListenerInternal(eventName, callback);
    }

    // 添加事件监听（带参数）
    public void AddListener<T>(string eventName, Action<T> callback)
    {
        AddListenerInternal(eventName, callback);
    }

    // 移除事件监听（无参数）
    public void RemoveListener(string eventName, Action callback)
    {
        RemoveListenerInternal(eventName, callback);
    }

    // 移除事件监听（带参数）
    public void RemoveListener<T>(string eventName, Action<T> callback)
    {
        RemoveListenerInternal(eventName, callback);
    }

    // 触发事件（无参数）
    public void TriggerEvent(string eventName)
    {
        TriggerEventInternal(eventName, null);
    }

    // 触发事件（带参数）
    public void TriggerEvent<T>(string eventName, T eventData)
    {
        TriggerEventInternal(eventName, eventData);
    }

    // 触发延迟事件（下一帧执行）
    public void TriggerEventDelayed(string eventName, object eventData = null, int framesDelay = 1)
    {
        delayedEvents.Enqueue(
            new DelayedEvent
            {
                eventName = eventName,
                eventData = eventData,
                framesDelay = framesDelay,
            }
        );
    }

    #endregion

    #region 内部实现

    private void AddListenerInternal(string eventName, Delegate callback)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("Event name cannot be null or empty!");
            return;
        }

        if (!eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] = new List<Delegate>();
        }

        // 检查是否已注册
        if (!eventDictionary[eventName].Contains(callback))
        {
            eventDictionary[eventName].Add(callback);

            if (enableDebug)
                Debug.Log(
                    $"Event listener added: {eventName}, Total: {eventDictionary[eventName].Count}"
                );
        }
    }

    private void RemoveListenerInternal(string eventName, Delegate callback)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName].Remove(callback);

            // 如果没有监听器了，清理这个事件
            if (eventDictionary[eventName].Count == 0)
            {
                eventDictionary.Remove(eventName);
            }

            if (enableDebug)
                Debug.Log($"Event listener removed: {eventName}");
        }
    }

    private void TriggerEventInternal(string eventName, object eventData)
    {
        if (isProcessingEvents)
        {
            // 如果正在处理事件，将新事件加入延迟队列
            TriggerEventDelayed(eventName, eventData, 1);
            return;
        }

        if (!eventDictionary.ContainsKey(eventName))
        {
            if (enableDebug)
                Debug.LogWarning($"No listeners for event: {eventName}");
            return;
        }

        isProcessingEvents = true;

        try
        {
            // 复制列表以避免在迭代过程中修改
            var callbacks = new List<Delegate>(eventDictionary[eventName]);

            foreach (var callback in callbacks)
            {
                try
                {
                    if (callback is Action action)
                    {
                        action.Invoke();
                    }
                    else if (eventData != null)
                    {
                        callback.DynamicInvoke(eventData);
                    }
                    else
                    {
                        // 尝试调用带默认参数的回调
                        var method = callback.Method;
                        var parameters = method.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType.IsValueType)
                        {
                            callback.DynamicInvoke(
                                Activator.CreateInstance(parameters[0].ParameterType)
                            );
                        }
                        else
                        {
                            callback.DynamicInvoke(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking event {eventName}: {ex.Message}");
                }
            }
        }
        finally
        {
            isProcessingEvents = false;
        }

        if (enableDebug)
            Debug.Log($"Event triggered: {eventName}");
    }

    #endregion

    #region 批量操作

    // 批量添加监听器
    public void AddListeners(Dictionary<string, Delegate> listeners)
    {
        foreach (var listener in listeners)
        {
            AddListenerInternal(listener.Key, listener.Value);
        }
    }

    // 移除某个事件的所有监听器
    public void RemoveAllListeners(string eventName)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary.Remove(eventName);

            if (enableDebug)
                Debug.Log($"All listeners removed for event: {eventName}");
        }
    }

    // 移除所有事件监听器
    public void RemoveAllListeners()
    {
        eventDictionary.Clear();

        if (enableDebug)
            Debug.Log("All event listeners cleared");
    }

    #endregion

    #region 查询和调试

    // 检查事件是否有监听器
    public bool HasListeners(string eventName)
    {
        return eventDictionary.ContainsKey(eventName) && eventDictionary[eventName].Count > 0;
    }

    // 获取事件的监听器数量
    public int GetListenerCount(string eventName)
    {
        return eventDictionary.ContainsKey(eventName) ? eventDictionary[eventName].Count : 0;
    }

    // 获取所有注册的事件
    public List<string> GetAllRegisteredEvents()
    {
        return new List<string>(eventDictionary.Keys);
    }

    // 打印事件统计信息
    public void PrintEventStatistics()
    {
        Debug.Log("=== Event Center Statistics ===");
        foreach (var pair in eventDictionary)
        {
            Debug.Log($"Event: {pair.Key}, Listeners: {pair.Value.Count}");
        }
        Debug.Log("===============================");
    }

    #endregion

    #region 更新处理

    private void Update()
    {
        ProcessDelayedEvents();
    }

    private void ProcessDelayedEvents()
    {
        int processedCount = 0;

        while (delayedEvents.Count > 0 && processedCount < maxEventsPerFrame)
        {
            var delayedEvent = delayedEvents.Peek();

            if (delayedEvent.framesDelay <= 0)
            {
                // 执行延迟事件
                delayedEvents.Dequeue();
                TriggerEventInternal(delayedEvent.eventName, delayedEvent.eventData);
                processedCount++;
            }
            else
            {
                // 减少延迟计数
                delayedEvent.framesDelay--;
                // 重新入队（由于是值类型，需要重新赋值）
                delayedEvents.Dequeue();
                delayedEvents.Enqueue(delayedEvent);
                processedCount++;
            }
        }
    }

    #endregion

    #region 清理

    public override void OnApplicationQuit()
    {
        RemoveAllListeners();
        delayedEvents.Clear();
    }

    #endregion
}

// 辅助类
public struct DelayedEvent
{
    public string eventName;
    public object eventData;
    public int framesDelay;
}

// 常用游戏事件定义
public static class GameEvents
{
    // 游戏流程事件
    public const string GAME_STARTED = "GameStarted";
    public const string GAME_PAUSED = "GamePaused";
    public const string GAME_RESUMED = "GameResumed";
    public const string GAME_OVER = "GameOver";
    public const string LEVEL_COMPLETED = "LevelCompleted";

    // UI事件
    public const string UI_PANEL_SHOW = "UIPanelShow";
    public const string UI_PANEL_HIDE = "UIPanelHide";
    public const string UI_BUTTON_CLICK = "UIButtonClick";

    // 战斗事件
    public const string BATTLE_START = "BattleStart";
    public const string BATTLE_END = "BattleEnd";
    public const string SKILL_USED = "SkillUsed";
    public const string UNIT_DEFEATED = "UnitDefeated";

    // 资源事件
    public const string ITEM_ACQUIRED = "ItemAcquired";
    public const string SPRITE_ADDED = "SpriteAdded";
    public const string RESOURCE_CHANGED = "ResourceChanged";

    // 地图和探索事件
    public const string MAP_NODE_SELECTED = "MapNodeSelected";
    public const string ROOM_ENTERED = "RoomEntered";
    public const string PATH_UNLOCKED = "PathUnlocked";
}
