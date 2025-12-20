using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI面板信息类
/// </summary>
[System.Serializable]
public class UIPanelInfo
{
    public string panelName;            // 面板名称
    public GameObject panelPrefab;      // 面板预制体
    public Transform parentTransform;   // 父级Transform（可选）
    public bool isPersistent = false;   // 是否常驻（不随场景销毁）
    public int layerIndex = 0;          // 层级索引（越大越在上面）
    
    [NonSerialized] public GameObject instance;  // 面板实例
    [NonSerialized] public bool isActive = false; // 是否激活
}

/// <summary>
/// UI管理器 - 单例模式
/// 管理所有UI面板的生成、显示、隐藏和层级
/// </summary>
public class UIManagerService : MonoBehaviour
{
    #region 单例实例
    
    private static UIManagerService _instance;
    public static UIManagerService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManagerService>();
                
                if (_instance == null)
                {
                    GameObject uiManager = new GameObject("UIManagerService");
                    _instance = uiManager.AddComponent<UIManagerService>();
                    DontDestroyOnLoad(uiManager);
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region UI面板配置
    
    [Header("UI面板配置")]
    [SerializeField] private List<UIPanelInfo> panelConfigs = new List<UIPanelInfo>();
    
    [Header("UI层级设置")]
    [SerializeField] private Transform[] uiLayers = new Transform[5];
    
    [Header("UI根Canvas")]
    [SerializeField] private Canvas rootCanvas;
    
    [Header("面板容器")]
    [SerializeField] private Transform panelsRoot; // 添加缺失的面板容器引用
    
    [Header("设置")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool enableRaycasting = false; // 控制是否启用事件拦截
    
    #endregion
    
    #region 私有变量
    
    private Dictionary<string, UIPanelInfo> _panelDictionary = new Dictionary<string, UIPanelInfo>();
    private List<UIPanelInfo> _activePanels = new List<UIPanelInfo>();
    private Stack<string> _panelHistory = new Stack<string>(); // 面板历史记录
    
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
        DontDestroyOnLoad(gameObject);

        // 自动注册 panelsRoot 下的所有子孙物体（递归）
        if (panelsRoot != null)
        {
            int count = 0;
            void RegisterRecursive(Transform t)
            {
                if (t == null)
                    return;
                var go = t.gameObject;
                RegisterPanel(go.name, go);
                count++;
                for (int i = 0; i < t.childCount; i++)
                {
                    RegisterRecursive(t.GetChild(i));
                }
            }
            RegisterRecursive(panelsRoot);
            if (debugMode)
                Debug.Log($"UIManager: 递归注册 {count} 个面板（含子孙）");
        }

        // 可选自动初始化（创建Canvas/层级 + 注册panelConfigs + 实例化常驻）
        if (autoInitialize)
        {
            Initialize();
        }
    }

    #endregion

    #region 初始化
    
    /// <summary>
    /// 初始化UI管理器
    /// </summary>
    public void Initialize()
    {
        if (debugMode) Debug.Log("UIManagerService 初始化开始");
        
        // 确保有根Canvas
        if (rootCanvas == null)
        {
            CreateRootCanvas();
        }
        
        // 确保有UI层级
        if (uiLayers.Length == 0)
        {
            CreateUILayers();
        }
        
        // 注册所有面板
        RegisterAllPanels();
        
        // 实例化常驻面板
        InstantiatePersistentPanels();
        
        if (debugMode) Debug.Log($"UIManagerService 初始化完成，注册面板数: {_panelDictionary.Count}");
    }
    
    /// <summary>
    /// 创建根Canvas
    /// </summary>
    private void CreateRootCanvas()
    {
        GameObject canvasObj = new GameObject("UI_RootCanvas");
        rootCanvas = canvasObj.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        
        // 默认不添加GraphicRaycaster，完全禁用事件拦截
        // GraphicRaycaster将在需要时通过SetRaycastingEnabled方法动态添加
        
        // 设置Canvas为子对象
        canvasObj.transform.SetParent(transform);
        
        if (debugMode) Debug.Log("创建根Canvas，事件拦截: 默认禁用");
    }
    
    /// <summary>
    /// 创建UI层级
    /// </summary>
    private void CreateUILayers()
    {
        uiLayers = new Transform[5];
        
        for (int i = 0; i < uiLayers.Length; i++)
        {
            GameObject layerObj = new GameObject($"Layer_{i}");
            layerObj.transform.SetParent(rootCanvas.transform);
            
            RectTransform rectTransform = layerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 添加CanvasGroup来控制事件拦截
            CanvasGroup canvasGroup = layerObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false; // 默认完全禁用事件拦截
            canvasGroup.interactable = false;   // 默认禁用交互
            
            uiLayers[i] = layerObj.transform;
            
            if (debugMode) Debug.Log($"创建UI层级: Layer_{i}, 事件拦截: 默认禁用");
        }
    }
    
    /// <summary>
    /// 注册所有面板
    /// </summary>
    private void RegisterAllPanels()
    {
        _panelDictionary.Clear();
        
        foreach (UIPanelInfo config in panelConfigs)
        {
            if (config.panelPrefab == null)
            {
                Debug.LogWarning($"面板 '{config.panelName}' 的预制体为空，跳过注册");
                continue;
            }
            
            if (_panelDictionary.ContainsKey(config.panelName))
            {
                Debug.LogWarning($"面板 '{config.panelName}' 已注册，跳过重复注册");
                continue;
            }
            
            _panelDictionary.Add(config.panelName, config);
            
            if (debugMode) Debug.Log($"注册面板: {config.panelName}");
        }
    }
    
    /// <summary>
    /// 实例化常驻面板
    /// </summary>
    private void InstantiatePersistentPanels()
    {
        foreach (var kvp in _panelDictionary)
        {
            if (kvp.Value.isPersistent)
            {
                InstantiatePanel(kvp.Value);
                
                // 默认隐藏
                kvp.Value.instance.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region 面板管理
    
    /// <summary>
    /// 显示面板
    /// </summary>
    public GameObject ShowPanel(string panelName, bool hidePrevious = true, bool addToHistory = true)
    {
        if (!_panelDictionary.ContainsKey(panelName))
        {
            // 特例：LoadingPanel 已废弃，直接忽略调用
            if (string.Equals(panelName, "LoadingPanel", StringComparison.Ordinal))
            {
                if (debugMode) Debug.Log("ShowPanel 调用了已废弃的 'LoadingPanel'，已忽略");
                return null;
            }
            // 尝试在场景中按名称查找并注册一个同名对象，降低配置缺失导致的阻断
            if (!TryResolveAndRegister(panelName))
            {
                Debug.LogError($"面板 '{panelName}' 未注册");
                return null;
            }
        }
        
        UIPanelInfo panelInfo = _panelDictionary[panelName];
        
        // 隐藏上一个面板（如果需要）
        if (hidePrevious && _activePanels.Count > 0)
        {
            HideTopPanel();
        }
        
        // 如果面板未实例化，先实例化
        if (panelInfo.instance == null)
        {
            InstantiatePanel(panelInfo);
        }
        
        // 激活面板
        panelInfo.instance.SetActive(true);
        panelInfo.isActive = true;
        
        // 设置层级
        SetPanelLayer(panelInfo);
        
        // 添加到活动面板列表
        if (!_activePanels.Contains(panelInfo))
        {
            _activePanels.Add(panelInfo);
        }
        
        // 添加到历史记录
        if (addToHistory)
        {
            _panelHistory.Push(panelName);
        }
        
        if (debugMode) Debug.Log($"显示面板: {panelName}");
        
        return panelInfo.instance;
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (!_panelDictionary.ContainsKey(panelName))
        {
            // 特例：LoadingPanel 已废弃，忽略隐藏请求
            if (string.Equals(panelName, "LoadingPanel", StringComparison.Ordinal))
            {
                if (debugMode) Debug.Log("HidePanel 调用了已废弃的 'LoadingPanel'，已忽略");
                return;
            }
            Debug.LogError($"面板 '{panelName}' 未注册");
            return;
        }
        
        UIPanelInfo panelInfo = _panelDictionary[panelName];
        
        if (panelInfo.instance != null && panelInfo.isActive)
        {
            panelInfo.instance.SetActive(false);
            panelInfo.isActive = false;
            
            // 从活动面板列表移除
            _activePanels.Remove(panelInfo);
            
            if (debugMode) Debug.Log($"隐藏面板: {panelName}");
        }
    }
    
    /// <summary>
    /// 隐藏最顶层的面板
    /// </summary>
    public void HideTopPanel()
    {
        if (_activePanels.Count > 0)
        {
            UIPanelInfo topPanel = _activePanels[_activePanels.Count - 1];
            HidePanel(topPanel.panelName);
            
            // 从历史记录移除
            if (_panelHistory.Count > 0)
            {
                _panelHistory.Pop();
            }
        }
    }
    
    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    public void HideAllPanels(bool excludePersistent = false)
    {
        List<UIPanelInfo> panelsToHide = new List<UIPanelInfo>(_activePanels);
        
        foreach (UIPanelInfo panel in panelsToHide)
        {
            if (excludePersistent && panel.isPersistent)
                continue;
                
            HidePanel(panel.panelName);
        }
        
        // 清空历史记录
        _panelHistory.Clear();
    }
    
    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel(string panelName)
    {
        if (!_panelDictionary.ContainsKey(panelName))
        {
            Debug.LogError($"面板 '{panelName}' 未注册");
            return;
        }
        
        UIPanelInfo panelInfo = _panelDictionary[panelName];
        
        if (panelInfo.isActive)
        {
            HidePanel(panelName);
        }
        else
        {
            ShowPanel(panelName, false);
        }
    }
    
    /// <summary>
    /// 返回上一个面板
    /// </summary>
    public void GoBack()
    {
        if (_panelHistory.Count > 1)
        {
            // 移除当前面板
            string currentPanel = _panelHistory.Pop();
            
            // 获取上一个面板
            string previousPanel = _panelHistory.Peek();
            
            // 显示上一个面板
            ShowPanel(previousPanel, false, false);
            
            // 隐藏当前面板
            HidePanel(currentPanel);
        }
    }
    
    /// <summary>
    /// 实例化面板
    /// </summary>
    private void InstantiatePanel(UIPanelInfo panelInfo)
    {
        if (panelInfo.panelPrefab == null)
        {
            Debug.LogError($"面板 '{panelInfo.panelName}' 的预制体为空，无法实例化");
            return;
        }
        
        // 确定父级Transform
        Transform parent = null;
        
        if (panelInfo.parentTransform != null)
        {
            parent = panelInfo.parentTransform;
        }
        else if (uiLayers.Length > panelInfo.layerIndex)
        {
            parent = uiLayers[panelInfo.layerIndex];
        }
        else
        {
            parent = rootCanvas.transform;
        }
        
        // 实例化面板
        panelInfo.instance = Instantiate(panelInfo.panelPrefab, parent);
        panelInfo.instance.name = panelInfo.panelName;
        
        // 不再自动设置全屏覆盖，保持预制体原有的尺寸和位置设置
        // 如果需要全屏面板，应该在预制体中设置好RectTransform
        
        if (debugMode) Debug.Log($"实例化面板: {panelInfo.panelName}");
    }
    
    /// <summary>
    /// 设置面板层级
    /// </summary>
    private void SetPanelLayer(UIPanelInfo panelInfo)
    {
        if (panelInfo.instance == null) return;
        
        Transform parent = null;
        
        if (panelInfo.parentTransform != null)
        {
            parent = panelInfo.parentTransform;
        }
        else if (uiLayers.Length > panelInfo.layerIndex)
        {
            parent = uiLayers[panelInfo.layerIndex];
        }
        
        if (parent != null)
        {
            panelInfo.instance.transform.SetParent(parent);
            panelInfo.instance.transform.SetAsLastSibling(); // 确保在最上面
        }
    }

    /// <summary>
    /// 兜底：在场景中查找同名 GameObject 作为面板并注册
    /// </summary>
    private bool TryResolveAndRegister(string panelName)
    {
        // 先在已知容器下找
        Transform found = null;
        if (panelsRoot != null)
        {
            found = FindChildRecursive(panelsRoot, panelName);
        }
        // 若未在容器下找到，尝试全局查找激活对象
        if (found == null)
        {
            var go = GameObject.Find(panelName);
            if (go != null)
                found = go.transform;
        }
        // 仍未找到，尝试包含未激活对象的全局查找
        if (found == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                var g = all[i];
                if (g != null && string.Equals(g.name, panelName, StringComparison.Ordinal))
                {
                    found = g.transform;
                    break;
                }
            }
        }
        if (found != null)
        {
            // 确保对象处于激活以便显示
            if (!found.gameObject.activeInHierarchy)
            {
                found.gameObject.SetActive(true);
            }
            RegisterPanel(panelName, found.gameObject);
            return true;
        }
        return false;
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (string.Equals(c.name, name, StringComparison.Ordinal))
                return c;
            var nested = FindChildRecursive(c, name);
            if (nested != null) return nested;
        }
        return null;
    }

    /// <summary>
    /// 确保指定面板已注册；若未注册则尝试查找并注册。
    /// </summary>
    public bool EnsurePanelRegistered(string panelName)
    {
        if (_panelDictionary.ContainsKey(panelName)) return true;
        return TryResolveAndRegister(panelName);
    }
    
    /// <summary>
    /// 获取面板实例
    /// </summary>
    public GameObject GetPanelInstance(string panelName)
    {
        if (_panelDictionary.ContainsKey(panelName))
        {
            return _panelDictionary[panelName].instance;
        }
        
        Debug.LogWarning($"面板 '{panelName}' 未找到");
        return null;
    }
    
    /// <summary>
    /// 检查面板是否显示
    /// </summary>
    public bool IsPanelActive(string panelName)
    {
        if (_panelDictionary.ContainsKey(panelName))
        {
            return _panelDictionary[panelName].isActive;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取当前活动的面板数量
    /// </summary>
    public int GetActivePanelCount()
    {
        return _activePanels.Count;
    }
    
    /// <summary>
    /// 清理所有面板
    /// </summary>
    private void ClearAllPanels()
    {
        foreach (var kvp in _panelDictionary)
        {
            if (kvp.Value.instance != null && !kvp.Value.isPersistent)
            {
                Destroy(kvp.Value.instance);
                kvp.Value.instance = null;
                kvp.Value.isActive = false;
            }
        }
        
        _activePanels.Clear();
        _panelHistory.Clear();
    }
    
    #endregion
    
    #region 注册和配置

    /// <summary>
    /// 注册新面板
    /// </summary>
    public void RegisterPanel(UIPanelInfo panelInfo)
    {
        if (_panelDictionary.ContainsKey(panelInfo.panelName))
        {
            Debug.LogWarning($"面板 '{panelInfo.panelName}' 已存在，将被覆盖");
        }
        
        _panelDictionary[panelInfo.panelName] = panelInfo;
        
        if (debugMode) Debug.Log($"注册新面板: {panelInfo.panelName}");
    }
    
    /// <summary>
    /// 从预制体动态注册面板
    /// </summary>
    public void RegisterPanelFromPrefab(string panelName, GameObject prefab, int layerIndex = 0, bool isPersistent = false)
    {
        UIPanelInfo panelInfo = new UIPanelInfo
        {
            panelName = panelName,
            panelPrefab = prefab,
            layerIndex = layerIndex,
            isPersistent = isPersistent
        };
        
        RegisterPanel(panelInfo);
    }

    /// <summary>
    /// 运行时注册已存在的面板 GameObject（兼容外部调用 RegisterPanel(name, gameObject)）
    /// 若已存在同名配置，将覆盖其 instance 并标记为已注册
    /// </summary>
    public void RegisterPanel(string panelName, GameObject panelGameObject)
    {
        if (string.IsNullOrEmpty(panelName) || panelGameObject == null)
        {
            if (debugMode) Debug.LogWarning($"RegisterPanel 参数无效 - {panelName}");
            return;
        }

        UIPanelInfo info;
        if (_panelDictionary.TryGetValue(panelName, out var existing))
        {
            info = existing;
        }
        else
        {
            info = new UIPanelInfo
            {
                panelName = panelName,
                panelPrefab = null,
                parentTransform = panelGameObject.transform.parent,
                isPersistent = false,
                layerIndex = 0
            };
            _panelDictionary[panelName] = info;
        }

        // 注册实例并设置激活状态
        info.instance = panelGameObject;
        info.isActive = panelGameObject.activeSelf;
        if (debugMode) Debug.Log($"运行时注册面板: {panelName} (来自已有 GameObject)");
    }

    /// <summary>
    /// 兼容旧接口：根据面板名返回 GameObject（GetPanel 的常见调用名）
    /// 委托给 GetPanelInstance 保持内部一致性
    /// </summary>
    public GameObject GetPanel(string panelName)
    {
        return GetPanelInstance(panelName);
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 游戏状态变化回调
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (debugMode) Debug.Log($"游戏状态变化: {newState}");
        
        switch (newState)
        {
            case GameState.Paused:
                // 暂停时确保某些UI可见
                break;
                
            case GameState.Playing:
                // 游戏进行中隐藏菜单UI
                if (GameManagerService.Instance.CurrentSceneType != SceneType.MainMenu)
                {
                    HidePanel("MainMenuPanel");
                }
                break;
                
            case GameState.GameOver:
                // 游戏结束时显示相应UI
                break;
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置UI事件拦截开关
    /// </summary>
    public void SetRaycastingEnabled(bool enabled)
    {
        enableRaycasting = enabled;
        
        // 更新根Canvas的GraphicRaycaster
        if (rootCanvas != null)
        {
            GraphicRaycaster raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (enabled && raycaster == null)
            {
                rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            else if (!enabled && raycaster != null)
            {
                Destroy(raycaster);
            }
        }
        
        // 更新所有UI层的CanvasGroup设置
        foreach (Transform layer in uiLayers)
        {
            if (layer != null)
            {
                CanvasGroup canvasGroup = layer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = enabled;
                    canvasGroup.interactable = enabled;
                }
            }
        }
        
        if (debugMode) Debug.Log($"UI事件拦截: {enabled}");
    }
    
    /// <summary>
    /// 显示消息提示
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        // 你可以创建一个专门的消息提示面板
        // 这里简单实现
        Debug.Log($"UI消息: {message}");
        
        // 示例：如果有MessagePanel就显示
        if (_panelDictionary.ContainsKey("MessagePanel"))
        {
            GameObject messagePanel = ShowPanel("MessagePanel", false);
            
            // 获取Text组件并设置消息
            Text messageText = messagePanel?.GetComponentInChildren<Text>();
            if (messageText != null)
            {
                messageText.text = message;
            }
            
            // 自动隐藏
            StartCoroutine(AutoHidePanel("MessagePanel", duration));
        }
    }
    
    /// <summary>
    /// 自动隐藏面板协程
    /// </summary>
    private System.Collections.IEnumerator AutoHidePanel(string panelName, float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePanel(panelName);
    }
    
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public void ShowConfirmDialog(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (_panelDictionary.ContainsKey("ConfirmDialog"))
        {
            GameObject dialog = ShowPanel("ConfirmDialog");
            
            // 这里需要根据你的确认对话框UI结构来设置
            // 例如：dialog.GetComponent<ConfirmDialog>().Setup(title, message, onConfirm, onCancel);
        }
    }
    
    #endregion
    
    #region 调试功能
    
    /// <summary>
    /// 打印UI状态
    /// </summary>
    public void PrintUIStatus()
    {
        Debug.Log("=== UI状态 ===");
        Debug.Log($"注册面板数: {_panelDictionary.Count}");
        Debug.Log($"活动面板数: {_activePanels.Count}");
        
        foreach (var kvp in _panelDictionary)
        {
            Debug.Log($"- {kvp.Key}: {(kvp.Value.isActive ? "显示" : "隐藏")}");
        }
        
        Debug.Log("==============");
    }
    
    #endregion
}
    