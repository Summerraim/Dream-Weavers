using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单的 UI 管理器：按名称注册和管理 UI 面板（GameObject）。
/// 供其他系统通过 UIManagerService.Instance 或反射调用（方法名：ShowPanel/HidePanel 等）。
/// </summary>
public class UIManagerService : MonoBehaviour
{
    [Header("自动注册")]
    [Tooltip("如果指定，Awake 时会把此 Transform 下的所有子物体（按名称）注册为面板")]
    public Transform panelsRoot;

    [Header("调试")]
    public bool debugMode = true;

    // 单例
    public static UIManagerService Instance { get; private set; }

    // 存储面板（按名称）
    private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>(
        StringComparer.OrdinalIgnoreCase
    );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动注册 panelsRoot 下的子物体
        if (panelsRoot != null)
        {
            for (int i = 0; i < panelsRoot.childCount; i++)
            {
                var child = panelsRoot.GetChild(i).gameObject;
                if (child != null)
                {
                    RegisterPanel(child.name, child);
                }
            }
            if (debugMode)
                Debug.Log($"UIManager: 注册 {panelsRoot.childCount} 个面板");
        }
    }

    #region 面板管理 API

    // 注册一个面板（覆盖已有同名）
    public void RegisterPanel(string name, GameObject panel)
    {
        if (string.IsNullOrEmpty(name) || panel == null)
            return;
        panels[name] = panel;
    }

    // 注销面板（如果存在）
    public void UnregisterPanel(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;
        panels.Remove(name);
    }

    // 获取面板（可能为 null）
    public GameObject GetPanel(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        panels.TryGetValue(name, out var panel);
        return panel;
    }

    // 显示面板（默认激活 GameObject）
    public void ShowPanel(string name)
    {
        SetPanelActive(name, true);
    }

    // 隐藏面板（默认禁用 GameObject）
    public void HidePanel(string name)
    {
        SetPanelActive(name, false);
    }

    // 切换面板可见性
    public void TogglePanel(string name)
    {
        var panel = GetPanel(name);
        if (panel == null)
            return;
        SetPanelActive(name, !panel.activeSelf);
    }

    // 显式设置面板激活状态
    public void SetPanelActive(string name, bool active)
    {
        var panel = GetPanel(name);
        if (panel == null)
        {
            if (debugMode)
                Debug.LogWarning($"UIManager: 未找到面板 '{name}'");
            return;
        }
        panel.SetActive(active);
    }

    // 查询面板是否激活
    public bool IsPanelActive(string name)
    {
        var panel = GetPanel(name);
        return panel != null && panel.activeSelf;
    }

    #endregion

    #region 辅助方法（编辑器与运行时方便使用）

    // 尝试按路径在场景中查找并注册一个面板
    public bool RegisterPanelByPath(string path)
    {
        var go = GameObject.Find(path);
        if (go != null)
        {
            RegisterPanel(go.name, go);
            return true;
        }
        return false;
    }

    // 清空所有注册（并不销毁 GameObject）
    public void ClearAllRegistrations()
    {
        panels.Clear();
    }

    #endregion
}
