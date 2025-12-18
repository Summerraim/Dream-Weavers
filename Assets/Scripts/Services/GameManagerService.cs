using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Menu,       // 主菜单状态
    Playing,    // 游戏状态（包括探索、对话等）
    Paused,     // 暂停状态
    GameOver    // 游戏结束状态
}

/// <summary>
/// 场景类型枚举
/// </summary>
public enum SceneType
{
    MainMenu,   // 主菜单场景
    Gameplay,   // 游戏场景（通用，用于未明确分类的场景）
    Battle,     // 战斗场景
    Level1,     // 第一关
    Level2,     // 第二关
    Level3,     // 第三关
    Level4      // 第四关
}

/// <summary>
/// 游戏管理器 - 单例模式
/// 管理游戏状态、场景切换和全局游戏逻辑
/// </summary>
public class GameManagerService : MonoBehaviour
{
    #region 单例实例
    
    private static GameManagerService _instance;
    private static bool _applicationIsQuitting = false;
    
    public static GameManagerService Instance
    {
        get
        {
            // 防止在应用退出时创建新实例
            if (_applicationIsQuitting)
            {
                return null;
            }
            
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManagerService>();
                
                if (_instance == null)
                {
                    GameObject gameManager = new GameObject("GameManagerService");
                    _instance = gameManager.AddComponent<GameManagerService>();
                    DontDestroyOnLoad(gameManager);
                }
            }
            return _instance;
        }
    }
    
    #endregion

    #region 事件委托
    
    // 游戏状态变化事件
    public event Action<GameState> OnGameStateChanged;
    
    // 场景加载事件
    public event Action<SceneType> OnSceneLoaded;
    public event Action<float> OnSceneLoadingProgress; // 加载进度事件
    
    #endregion
    
    #region 公有属性
    
    [Header("游戏状态")]
    [SerializeField] private GameState _currentGameState = GameState.Menu;
    
    public GameState CurrentGameState
    {
        get => _currentGameState;
        private set
        {
            if (_currentGameState != value)
            {
                GameState previousState = _currentGameState;
                _currentGameState = value;
                
                // 处理状态变化逻辑
                HandleStateChange(previousState, value);
                
                // 触发事件
                OnGameStateChanged?.Invoke(value);
            }
        }
    }
    
    [Header("场景管理")]
    [SerializeField] private SceneType _currentSceneType = SceneType.MainMenu;
    public SceneType CurrentSceneType => _currentSceneType;
    
    [Header("游戏数据")]
    public int currentLevel = 1;
    public bool isNewGame = true;
    
    [Header("暂停设置")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool canPause = true;
    
    #endregion
    
    #region 私有变量
    
    private bool _isLoadingScene = false;
    private AsyncOperation _currentAsyncOperation;
    private Coroutine _loadingCoroutine;
    
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
        
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }
    
    private void Start()
    {
        // 初始化设置
        Initialize();
    }
    
    private void Update()
    {
        // 处理全局输入
        HandleGlobalInput();
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
    }
    
    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
    
    #endregion
    
    #region 初始化
    
    private void Initialize()
    {
        // 根据当前场景设置初始状态
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName.Contains("Menu") || sceneName == "MainMenu")
        {
            _currentSceneType = SceneType.MainMenu;
            SetGameState(GameState.Menu);
        }
        else if (sceneName.Contains("Battle"))
        {
            _currentSceneType = SceneType.Battle;
            SetGameState(GameState.Playing);
        }
        else if (sceneName.Contains("Level1") || sceneName.Contains("level1"))
        {
            _currentSceneType = SceneType.Level1;
            SetGameState(GameState.Playing);
        }
        else if (sceneName.Contains("Level2") || sceneName.Contains("level2"))
        {
            _currentSceneType = SceneType.Level2;
            SetGameState(GameState.Playing);
        }
        else if (sceneName.Contains("Level3") || sceneName.Contains("level3"))
        {
            _currentSceneType = SceneType.Level3;
            SetGameState(GameState.Playing);
        }
        else if (sceneName.Contains("Level4") || sceneName.Contains("level4"))
        {
            _currentSceneType = SceneType.Level4;
            SetGameState(GameState.Playing);
        }
        else
        {
            _currentSceneType = SceneType.Gameplay;
            SetGameState(GameState.Playing);
        }
        
        Debug.Log($"GameManager初始化完成 - 场景: {_currentSceneType}, 状态: {CurrentGameState}");
    }
    
    #endregion
    
    #region 游戏状态控制
    
    /// <summary>
    /// 设置游戏状态
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (_isLoadingScene && newState != GameState.Menu) return;
        
        CurrentGameState = newState;
    }
    
    /// <summary>
    /// 处理状态变化
    /// </summary>
    private void HandleStateChange(GameState previousState, GameState newState)
    {
        Debug.Log($"游戏状态变更: {previousState} -> {newState}");
        
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                // 只在战斗场景中锁定和隐藏光标，其他场景保持光标可见
                if (_currentSceneType == SceneType.Battle)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                // 游戏进行中禁用UI事件拦截，避免阻挡鼠标事件
                if (UIManagerService.Instance != null)
                {
                    UIManagerService.Instance.SetRaycastingEnabled(false);
                }
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // 暂停时启用UI事件拦截，允许UI交互
                if (UIManagerService.Instance != null)
                {
                    UIManagerService.Instance.SetRaycastingEnabled(true);
                }
                break;
                
            case GameState.Menu:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // 菜单场景启用UI事件拦截，允许UI交互
                if (UIManagerService.Instance != null)
                {
                    UIManagerService.Instance.SetRaycastingEnabled(true);
                }
                break;
                
            case GameState.GameOver:
                Time.timeScale = 0.5f; // 慢动作效果
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // 游戏结束时启用UI事件拦截，允许UI交互
                if (UIManagerService.Instance != null)
                {
                    UIManagerService.Instance.SetRaycastingEnabled(true);
                }
                break;
        }
    }
    
    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (!canPause || _isLoadingScene) return;
        
        if (CurrentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (CurrentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }
    
    #endregion
    
    #region 场景管理
    
    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScene(SceneType sceneType, int levelIndex = 1)
    {
        if (_isLoadingScene) return;
        
        string sceneName = GetSceneNameByType(sceneType);
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"未找到场景类型: {sceneType}");
            return;
        }
        
        _isLoadingScene = true;
        _currentSceneType = sceneType;
        currentLevel = levelIndex;
        
        // 显示加载界面
        UIManagerService.Instance?.ShowPanel("LoadingPanel");
        
        // 开始加载场景
        _loadingCoroutine = StartCoroutine(LoadSceneAsync(sceneName));
    }
    
    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        isNewGame = true;
        currentLevel = 1;
        
        // 加载游戏场景
        LoadScene(SceneType.Gameplay, 1);
    }
    
    /// <summary>
    /// 加载战斗场景
    /// </summary>
    public void LoadBattleScene(int battleLevel = 1)
    {
        LoadScene(SceneType.Battle, battleLevel);
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        LoadScene(SceneType.MainMenu);
    }
    
    /// <summary>
    /// 重新开始当前关卡
    /// </summary>
    public void RestartCurrentLevel()
    {
        LoadScene(_currentSceneType, currentLevel);
    }
    
    /// <summary>
    /// 异步加载场景
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 等待一帧确保UI更新
        yield return null;
        
        // 开始异步加载
        _currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName);
        _currentAsyncOperation.allowSceneActivation = false;
        
        float progress = 0;
        
        // 等待加载
        while (!_currentAsyncOperation.isDone)
        {
            progress = Mathf.Clamp01(_currentAsyncOperation.progress / 0.9f);
            
            // 更新加载进度
            OnSceneLoadingProgress?.Invoke(progress);
            
            // 模拟加载延迟，确保加载界面可见
            if (_currentAsyncOperation.progress >= 0.9f)
            {
                // 等待额外时间（可选）
                yield return new WaitForSeconds(0.5f);
                
                // 激活场景
                _currentAsyncOperation.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        _isLoadingScene = false;
        _currentAsyncOperation = null;
    }
    
    /// <summary>
    /// 根据场景类型获取场景名称
    /// </summary>
    private string GetSceneNameByType(SceneType sceneType)
    {
        // 这里需要根据你的实际场景命名来设置
        switch (sceneType)
        {
            case SceneType.MainMenu:
                return "MainMenuScene"; // 你的主菜单场景名称
            case SceneType.Gameplay:
                return $"GameplayLevel{currentLevel}"; // 游戏场景名称
            case SceneType.Battle:
                return $"BattleScene{currentLevel}"; // 战斗场景名称
            case SceneType.Level1:
                return "Level1Scene"; // 第一关场景名称
            case SceneType.Level2:
                return "Level2Scene"; // 第二关场景名称
            case SceneType.Level3:
                return "Level3Scene"; // 第三关场景名称
            case SceneType.Level4:
                return "Level4Scene"; // 第四关场景名称
            default:
                return "";
        }
    }
    
    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成: {scene.name}");
        
        // 根据场景类型设置游戏状态
        if (_currentSceneType == SceneType.MainMenu)
        {
            SetGameState(GameState.Menu);
        }
        else
        {
            SetGameState(GameState.Playing);
        }
        
        // 触发场景加载事件
        OnSceneLoaded?.Invoke(_currentSceneType);
        
        // 隐藏加载界面
        UIManagerService.Instance?.HidePanel("LoadingPanel");
        
        // 显示适当的UI
        ShowSceneUI();
    }
    
    /// <summary>
    /// 显示场景对应的UI
    /// </summary>
    private void ShowSceneUI()
    {
        switch (_currentSceneType)
        {
            case SceneType.MainMenu:
                UIManagerService.Instance?.ShowPanel("MainMenuPanel");
                break;
            case SceneType.Gameplay:
                UIManagerService.Instance?.ShowPanel("HUD");
                UIManagerService.Instance?.ShowPanel("MinimapPanel");
                break;
            case SceneType.Battle:
                UIManagerService.Instance?.ShowPanel("BattleHUD");
                UIManagerService.Instance?.ShowPanel("UnitInfoPanel");
                break;
        }
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理全局输入
    /// </summary>
    private void HandleGlobalInput()
    {
        // ESC键暂停游戏
        if (Input.GetKeyDown(pauseKey) && canPause)
        {
            // 只有在游戏或暂停状态下才能切换
            if (CurrentGameState == GameState.Playing || CurrentGameState == GameState.Paused)
            {
                TogglePause();
            }
        }
        
        // 开发快捷键
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            LoadScene(SceneType.MainMenu);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            LoadScene(SceneType.Gameplay, 1);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            LoadScene(SceneType.Battle, 1);
        }
        #endif
    }
    
    #endregion
    
    #region 游戏流程控制
    
    /// <summary>
    /// 游戏胜利
    /// </summary>
    public void GameWin()
    {
        Debug.Log("游戏胜利！");
        
        // 显示胜利界面
        UIManagerService.Instance?.ShowPanel("VictoryPanel");
        
        // 可以在这里保存游戏进度
    }
    
    /// <summary>
    /// 游戏失败
    /// </summary>
    public void GameLose()
    {
        Debug.Log("游戏失败！");
        
        // 显示失败界面
        UIManagerService.Instance?.ShowPanel("GameOverPanel");
        
        SetGameState(GameState.GameOver);
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 获取场景加载进度
    /// </summary>
    public float GetLoadingProgress()
    {
        if (_currentAsyncOperation != null)
        {
            return Mathf.Clamp01(_currentAsyncOperation.progress / 0.9f);
        }
        return 0f;
    }
    
    /// <summary>
    /// 检查是否正在加载场景
    /// </summary>
    public bool IsLoadingScene()
    {
        return _isLoadingScene;
    }
    
    #endregion
}
