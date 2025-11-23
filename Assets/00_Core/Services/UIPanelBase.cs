using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI面板基类 - 所有UI面板都要继承这个类
public abstract class UIPanelBase : MonoBehaviour
{
    [Header("Panel Settings")]
    public string panelName;
    public bool isPersistent = false; // 是否持久化（不随场景切换销毁）
    public bool hideUnderlying = true; // 是否隐藏下层面板
    public int sortOrder = 0; // 渲染顺序
    
    public virtual void Initialize() { }
    public virtual void OnShow() { }
    public virtual void OnHide() { }
    public virtual void OnBackButton() { }
}

// UI面板状态
public enum UIPanelState
{
    Hidden,
    Showing,
    Visible,
    Hiding
}

// UI事件定义
public static class UIEvents
{
    public const string PANEL_SHOW_START = "PanelShowStart";
    public const string PANEL_SHOW_COMPLETE = "PanelShowComplete";
    public const string PANEL_HIDE_START = "PanelHideStart";
    public const string PANEL_HIDE_COMPLETE = "PanelHideComplete";
    public const string UI_STACK_CHANGED = "UIStackChanged";
}
