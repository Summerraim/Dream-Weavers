using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// 游戏状态枚举
public enum GameState
{
    MainMenu,       // 主菜单
    InGame,         // 游戏中
    Paused,         // 暂停
    GameOver,       // 游戏结束
    Loading         // 加载中
}

// 将类改为直接继承 MonoBehaviour 并实现本类单例，避免对外部 Singleton<T> 的编译依赖
public class GameManagerService : MonoBehaviour
{
    // 本类单例
    public static GameManagerService Instance { get; private set; }

    // 当前游戏状态
    public GameState CurrentState { get; private set; }
    
    // 游戏状态变化事件
    public event Action<GameState, GameState> OnGameStateChanged;
    
    [Header("游戏配置")]
    public bool debugMode = true;
    public float gameSpeed = 1.0f;
    
    [Header("场景配置")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "GameScene";
    
    // Service 引用（使用 object + 反射访问以消除编译期依赖）
    private object eventCenter;
    private object uiManager;
    private object sceneManager;
    private object audioManager;
    
    // 游戏数据
    private bool isInitialized = false;

    // 事件监听委托引用（用于移除监听）
    private Action uiButtonListener;
    private Action gameOverListener;
    private Action levelCompletedListener;
    private Action requestPauseListener;
    private Action requestResumeListener;
    private Action requestQuitListener;

    private void Awake()
    {
        // 简单单例实现
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeGame();
    }

    private void InitializeGame()
    {
        if (isInitialized) return;
        
        // 尝试通过反射获取 Service 实例（如果存在）
        eventCenter = GetSingletonInstance("EventCenterService");
        uiManager = GetSingletonInstance("UIManagerService");
        sceneManager = GetSingletonInstance("SceneManagerService");
        audioManager = GetSingletonInstance("AudioManagerService");
        
        // 直接执行初始化步骤（原先通过协程等待一帧以保证其他 Awake 完成）
        // 如果需要严格的一帧延迟，请在外部调用或改回协程方式。
        InitializeServices();
    }

    private void InitializeServices()
    {
        // 注册事件监听
        RegisterEvents();
        
        // 设置初始状态
        SwitchState(GameState.MainMenu);
        
        // 设置目标帧率
        Application.targetFrameRate = 60;
        
        isInitialized = true;
        
        if (debugMode) Debug.Log("GameManager: 所有Service初始化完成");
    }

    private void RegisterEvents()
    {
        // 使用字符串事件名，避免依赖不存在的 GameEvents 常量类
        uiButtonListener = () => OnUIButtonClick(null);
        InvokeServiceMethod(eventCenter, "AddListener", "UI_BUTTON_CLICK", uiButtonListener);
        
        gameOverListener = () => OnGameOver(null);
        levelCompletedListener = () => OnLevelCompleted(null);
        InvokeServiceMethod(eventCenter, "AddListener", "GAME_OVER", gameOverListener);
        InvokeServiceMethod(eventCenter, "AddListener", "LEVEL_COMPLETED", levelCompletedListener);

        requestPauseListener = () => OnPauseRequested(null);
        requestResumeListener = () => OnResumeRequested(null);
        requestQuitListener = () => OnQuitToMenuRequested(null);
        InvokeServiceMethod(eventCenter, "AddListener", "RequestPause", requestPauseListener);
        InvokeServiceMethod(eventCenter, "AddListener", "RequestResume", requestResumeListener);
        InvokeServiceMethod(eventCenter, "AddListener", "RequestQuitToMenu", requestQuitListener);
    }

    #region 状态管理核心方法

    // 核心方法：切换游戏状态
    public void SwitchState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        if (!isInitialized)
        {
            Debug.LogWarning("GameManager not initialized yet!");
            return;
        }

        GameState previousState = CurrentState;
        CurrentState = newState;

        // 执行状态进入和退出逻辑
        HandleStateExit(previousState);
        HandleStateEnter(newState);

        // 触发状态变化事件
        OnGameStateChanged?.Invoke(previousState, newState);
        InvokeServiceMethod(eventCenter, "TriggerEvent", "GameStateChanged", new { previous = previousState, current = newState });

        if (debugMode) 
            Debug.Log($"GameState: {previousState} -> {newState}");
    }

    private void HandleStateExit(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                // 暂停状态退出时的清理
                InvokeServiceMethod(audioManager, "ResumeSFX");
                break;
                
            case GameState.InGame:
                // 游戏状态退出时的处理
                break;
        }
    }

    private void HandleStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                EnterMainMenuState();
                break;
            case GameState.InGame:
                EnterInGameState();
                break;
            case GameState.Paused:
                EnterPausedState();
                break;
            case GameState.GameOver:
                EnterGameOverState();
                break;
            case GameState.Loading:
                EnterLoadingState();
                break;
        }

        UpdateTimeScale();
    }

    #endregion

    #region 状态具体实现

    private void EnterMainMenuState()
    {
        // 显示主菜单界面
        InvokeServiceMethod(uiManager, "ShowPanel", "MainMenu");
        InvokeServiceMethod(uiManager, "HidePanel", "HUD");
        InvokeServiceMethod(uiManager, "HidePanel", "PauseMenu");
        
        // 播放主菜单音乐
        InvokeServiceMethod(audioManager, "PlayBGM", "MainMenuBGM");
        
        // 重置游戏速度
        gameSpeed = 1.0f;
    }

    private void EnterInGameState()
    {
        // 显示游戏HUD
        InvokeServiceMethod(uiManager, "ShowPanel", "HUD");
        InvokeServiceMethod(uiManager, "HidePanel", "MainMenu");
        InvokeServiceMethod(uiManager, "HidePanel", "PauseMenu");
        
        // 播放游戏背景音乐
        InvokeServiceMethod(audioManager, "PlayBGM", "GameBGM");
        
        // 触发游戏开始事件
        InvokeServiceMethod(eventCenter, "TriggerEvent", "GAME_STARTED");
    }

    private void EnterPausedState()
    {
        // 显示暂停菜单
        InvokeServiceMethod(uiManager, "ShowPanel", "PauseMenu");
        
        // 暂停游戏音效（背景音乐继续）
        InvokeServiceMethod(audioManager, "PauseSFX");
    }

    private void EnterGameOverState()
    {
        // 显示游戏结束界面
        InvokeServiceMethod(uiManager, "ShowPanel", "GameOver");
        
        // 播放游戏结束音效
        InvokeServiceMethod(audioManager, "PlaySFX", "GameOver");
        
        // 保存游戏数据（通过反射调用，避免编译时依赖）
        TryInvokeSingletonMethod("SaveLoadManagerService", "SaveGame");
    }

    private void EnterLoadingState()
    {
        // 显示加载界面
        InvokeServiceMethod(uiManager, "ShowPanel", "Loading");
    }

    #endregion

    #region 公共方法 - 供其他系统调用

    // 开始新游戏
    public void StartNewGame()
    {
        if (debugMode) Debug.Log("开始新游戏");
        
        // 重置游戏数据（使用反射，避免编译时依赖）
        TryInvokeSingletonMethod("RoguelikeProgressionController", "ResetRun");
        
        // 加载游戏场景（这里同步触发 LoadSceneAsync，如果需要等待加载完成请改回协程或在 SceneManagerService 内处理回调）
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        SwitchState(GameState.Loading);
        
        if (sceneManager != null)
        {
            // 尝试调用 LoadSceneAsync（如果存在）
            InvokeServiceMethod(sceneManager, "LoadSceneAsync", gameScene);
        }
        else
        {
            if (debugMode) Debug.LogWarning("SceneManagerService 未就绪，LoadSceneAsync 无法调用");
        }
        
        // 场景加载被触发后进入游戏状态（注意：如果实际加载是异步并需要等待，建议恢复协程实现）
        SwitchState(GameState.InGame);
    }

    // 继续游戏
    public void ContinueGame()
    {
        // 使用反射安全检查是否存在存档，避免编译期依赖
        bool hasSave = InvokeSingletonBoolMethod("SaveLoadManagerService", "HasSaveData");
        if (hasSave)
        {
            LoadGameScene();
        }
        else
        {
            StartNewGame();
        }
    }

    // 暂停游戏
    public void PauseGame()
    {
        if (CurrentState == GameState.InGame)
        {
            SwitchState(GameState.Paused);
            InvokeServiceMethod(eventCenter, "TriggerEvent", "GAME_PAUSED");
        }
    }

    // 恢复游戏
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            SwitchState(GameState.InGame);
            InvokeServiceMethod(eventCenter, "TriggerEvent", "GAME_RESUMED");
        }
    }

    // 退出到主菜单
    public void QuitToMainMenu()
    {
        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        SwitchState(GameState.Loading);
        
        // 保存游戏进度
        TryInvokeSingletonMethod("SaveLoadManagerService", "SaveGame");
        
        if (sceneManager != null)
        {
            InvokeServiceMethod(sceneManager, "LoadSceneAsync", mainMenuScene);
        }
        else
        {
            if (debugMode) Debug.LogWarning("SceneManagerService 未就绪，LoadSceneAsync 无法调用");
        }
        
        SwitchState(GameState.MainMenu);
    }

    // 退出游戏
    public void QuitGame()
    {
        // 保存游戏
        TryInvokeSingletonMethod("SaveLoadManagerService", "SaveGame");
        
        if (debugMode) Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 事件处理

    private void OnUIButtonClick(object data)
    {
        // 处理UI按钮点击事件
        if (debugMode) Debug.Log($"UI Button Clicked: {data}");
    }

    private void OnPauseRequested(object data)
    {
        PauseGame();
    }

    private void OnResumeRequested(object data)
    {
        ResumeGame();
    }

    private void OnQuitToMenuRequested(object data)
    {
        QuitToMainMenu();
    }

    private void OnGameOver(object data)
    {
        SwitchState(GameState.GameOver);
    }

    private void OnLevelCompleted(object data)
    {
        // 关卡完成逻辑
        if (debugMode) Debug.Log("关卡完成！");
        
        InvokeServiceMethod(eventCenter, "TriggerEvent", "LevelTransitionStart");
    }

    #endregion

    #region 工具方法

    // 更新游戏时间缩放（用于暂停）
    private void UpdateTimeScale()
    {
        switch (CurrentState)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.InGame:
                Time.timeScale = gameSpeed;
                break;
            default:
                Time.timeScale = 1f;
                break;
        }
    }

    // 设置游戏速度（用于加速模式等）
    public void SetGameSpeed(float speed)
    {
        gameSpeed = Mathf.Clamp(speed, 0.1f, 3f);
        if (CurrentState == GameState.InGame)
        {
            Time.timeScale = gameSpeed;
        }
    }

    // 检查是否在游戏中
    public bool IsInGame()
    {
        return CurrentState == GameState.InGame;
    }

    // 检查游戏是否暂停
    public bool IsPaused()
    {
        return CurrentState == GameState.Paused;
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        // 清理事件监听（使用保存的委托引用移除）
        if (eventCenter != null)
        {
            InvokeServiceMethod(eventCenter, "RemoveListener", "UI_BUTTON_CLICK", uiButtonListener);
            InvokeServiceMethod(eventCenter, "RemoveListener", "GAME_OVER", gameOverListener);
            InvokeServiceMethod(eventCenter, "RemoveListener", "LEVEL_COMPLETED", levelCompletedListener);
            InvokeServiceMethod(eventCenter, "RemoveListener", "RequestPause", requestPauseListener);
            InvokeServiceMethod(eventCenter, "RemoveListener", "RequestResume", requestResumeListener);
            InvokeServiceMethod(eventCenter, "RemoveListener", "RequestQuitToMenu", requestQuitListener);
        }

        if (Instance == this) Instance = null;
    }

    #endregion

    #region 反射辅助（安全调用外部单例方法与服务方法）

    // 尝试根据类型名查找并返回静态 Instance（或 null）
    private object GetSingletonInstance(string typeName)
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.Name == typeName || t.FullName == typeName)
                    {
                        var prop = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        if (prop != null) return prop.GetValue(null);
                        var field = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                        if (field != null) return field.GetValue(null);
                        // 如果没有 Instance 字段/属性，尝试返回类型的静态属性/字段名为 "instance"（小写）
                        prop = t.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (prop != null) return prop.GetValue(null);
                        field = t.GetField("instance", BindingFlags.Public | BindingFlags.Static);
                        if (field != null) return field.GetValue(null);
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (debugMode) Debug.LogError($"GetSingletonInstance({typeName}) 异常: {ex}");
        }
        return null;
    }

    // 在已获取的 service 实例上调用方法（不关心返回值）
    private void InvokeServiceMethod(object service, string methodName, params object[] args)
    {
        if (service == null) return;
        try
        {
            var t = service.GetType();
            // 尝试精确匹配参数个数的公共/非公共方法
            MethodInfo method = null;
            foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != methodName) continue;
                var ps = m.GetParameters();
                if (ps.Length != args.Length) continue;
                method = m;
                break;
            }
            if (method == null)
            {
                if (debugMode) Debug.LogWarning($"{t.Name} 中未找到方法 {methodName}({args.Length} args)");
                return;
            }
            method.Invoke(service, args);
        }
        catch (Exception ex)
        {
            if (debugMode) Debug.LogError($"调用服务方法 {methodName} 异常: {ex}");
        }
    }

    // 通过反射尝试在指定单例类型上调用无参方法（安全、不会引入编译期依赖）
    private void TryInvokeSingletonMethod(string typeName, string methodName)
    {
        try
        {
            var instance = GetSingletonInstance(typeName);
            if (instance == null)
            {
                if (debugMode) Debug.LogWarning($"{typeName} 类型或其实例未找到，无法调用 {methodName}()");
                return;
            }

            var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                if (debugMode) Debug.LogWarning($"{typeName} 中未找到方法 {methodName}()");
                return;
            }

            method.Invoke(instance, null);
        }
        catch (Exception ex)
        {
            if (debugMode) Debug.LogError($"调用 {typeName}.{methodName} 时发生异常: {ex}");
        }
    }

    // 通过反射尝试在指定单例类型上调用无参方法并返回 bool（用于 HasSaveData）
    private bool InvokeSingletonBoolMethod(string typeName, string methodName)
    {
        try
        {
            var instance = GetSingletonInstance(typeName);
            if (instance == null)
            {
                if (debugMode) Debug.LogWarning($"{typeName} 类型或其实例未找到，无法调用 {methodName}()");
                return false;
            }
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                if (debugMode) Debug.LogWarning($"{typeName} 中未找到方法 {methodName}()");
                return false;
            }
            var result = method.Invoke(instance, null);
            return result is bool b && b;
        }
        catch (Exception ex)
        {
            if (debugMode) Debug.LogError($"调用 {typeName}.{methodName} 时发生异常: {ex}");
            return false;
        }
    }

    #endregion
}
