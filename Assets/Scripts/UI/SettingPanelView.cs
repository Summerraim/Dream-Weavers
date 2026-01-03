using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏内设置面板（非主菜单场景使用）
/// - 按 ESC 键打开/关闭
/// - 控制音量设置
/// - 提供退出游戏按钮
/// </summary>
public class SettingPanelView : MonoBehaviour
{
    [Header("面板设置")]
    [SerializeField]
    private GameObject settingsPanel;

    [Header("音量控制")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private Slider musicVolumeSlider;

    [SerializeField]
    private Slider sfxVolumeSlider;

    [Header("按钮")]
    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button quitGameButton;

    [Header("音频管理器引用")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("退出设置")]
    [Tooltip("退出时返回的场景名称（留空则直接退出应用程序）")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [Tooltip("是否返回主菜单（false则直接退出应用程序）")]
    [SerializeField]
    private bool returnToMainMenu = true;

    // 面板是否打开
    private bool isPanelOpen = false;

    // 存储临时的音量设置（用于取消时恢复）
    private float tempMasterVolume;
    private float tempMusicVolume;
    private float tempSfxVolume;

    private void Awake()
    {
        // 确保音频管理器存在
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("[SettingPanelView] 未找到 AudioManager");
            }
        }

        // 初始时隐藏面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }
        else
        {
            Debug.LogError("[SettingPanelView] settingsPanel 未配置！");
        }
    }

    private void Start()
    {
        // 绑定按钮事件
        BindButtonEvents();

        // 加载音量设置
        LoadVolumeSettings();
    }

    private void Update()
    {
        // 监听 ESC 键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    private void BindButtonEvents()
    {
        // 关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        // 保存按钮
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        // 退出游戏按钮
        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveAllListeners();
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
        }

        // 音量滑块事件（实时预览）
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    #region 面板控制

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (isPanelOpen)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void ShowPanel()
    {
        if (settingsPanel == null)
            return;

        // 保存当前音量设置到临时变量（用于取消时恢复）
        SaveCurrentVolumeToTemp();

        // 显示面板
        settingsPanel.SetActive(true);
        isPanelOpen = true;

        // 暂停游戏（可选）
        Time.timeScale = 0f;

        Debug.Log("[SettingPanelView] 设置面板已打开");
    }

    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public void HidePanel()
    {
        if (settingsPanel == null)
            return;

        // 隐藏面板
        settingsPanel.SetActive(false);
        isPanelOpen = false;

        // 恢复游戏时间（如果之前暂停了）
        Time.timeScale = 1f;

        Debug.Log("[SettingPanelView] 设置面板已关闭");
    }

    /// <summary>
    /// 检查面板是否打开
    /// </summary>
    public bool IsOpen()
    {
        return isPanelOpen;
    }

    #endregion

    #region 按钮回调

    private void OnCloseClicked()
    {
        PlayButtonClickSound();

        // 不保存直接关闭（恢复之前的音量）
        RestoreVolumeFromTemp();

        HidePanel();
    }

    private void OnSaveClicked()
    {
        PlayButtonClickSound();

        // 保存音量设置
        SaveVolumeSettings();

        // 提示保存成功（可选）
        Debug.Log("[SettingPanelView] 设置已保存");

        // 关闭面板
        HidePanel();
    }

    private void OnQuitGameClicked()
    {
        PlayButtonClickSound();

        // 恢复游戏时间
        Time.timeScale = 1f;

        if (returnToMainMenu && !string.IsNullOrEmpty(mainMenuSceneName))
        {
            // 返回主菜单
            Debug.Log($"[SettingPanelView] 返回主菜单: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // 直接退出应用程序
            Debug.Log("[SettingPanelView] 退出游戏");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    #endregion

    #region 音量控制

    private void OnMasterVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器
        if (audioManager != null)
        {
            audioManager.SetBGMVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(value);
        }
    }

    #endregion

    #region 音量设置管理

    private void SaveCurrentVolumeToTemp()
    {
        // 保存当前音量到临时变量
        if (masterVolumeSlider != null)
            tempMasterVolume = masterVolumeSlider.value;
        if (musicVolumeSlider != null)
            tempMusicVolume = musicVolumeSlider.value;
        if (sfxVolumeSlider != null)
            tempSfxVolume = sfxVolumeSlider.value;

        Debug.Log(
            $"[SettingPanelView] 保存临时音量: Master={tempMasterVolume:F2}, Music={tempMusicVolume:F2}, SFX={tempSfxVolume:F2}"
        );
    }

    private void RestoreVolumeFromTemp()
    {
        // 恢复之前保存的音量
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = tempMasterVolume;
            if (audioManager != null)
                audioManager.SetMasterVolume(tempMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = tempMusicVolume;
            if (audioManager != null)
                audioManager.SetBGMVolume(tempMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = tempSfxVolume;
            if (audioManager != null)
                audioManager.SetSFXVolume(tempSfxVolume);
        }

        Debug.Log("[SettingPanelView] 已恢复临时音量");
    }

    private void LoadVolumeSettings()
    {
        // 从PlayerPrefs加载保存的音量设置
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // 更新UI滑块
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;

        // 应用音量设置到音频管理器
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(masterVolume);
            audioManager.SetBGMVolume(musicVolume);
            audioManager.SetSFXVolume(sfxVolume);
        }

        Debug.Log(
            $"[SettingPanelView] 加载音量设置: Master={masterVolume:F2}, Music={musicVolume:F2}, SFX={sfxVolume:F2}"
        );
    }

    private void SaveVolumeSettings()
    {
        // 从滑块获取当前值
        float masterVolume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
        float musicVolume = musicVolumeSlider != null ? musicVolumeSlider.value : 1f;
        float sfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : 1f;

        // 保存到PlayerPrefs
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();

        // 应用音量设置到音频管理器（确保设置被应用）
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(masterVolume);
            audioManager.SetBGMVolume(musicVolume);
            audioManager.SetSFXVolume(sfxVolume);
        }

        Debug.Log(
            $"[SettingPanelView] 保存音量设置: Master={masterVolume:F2}, Music={musicVolume:F2}, SFX={sfxVolume:F2}"
        );
    }

    #endregion

    #region 工具方法

    private void PlayButtonClickSound()
    {
        if (audioManager != null)
        {
            // 这里可以添加按钮点击音效
            // 需要先配置音效文件，然后调用 audioManager.PlaySFX(buttonClickSound);
            Debug.Log("[SettingPanelView] 播放按钮点击音效");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // 清理事件绑定，防止内存泄漏
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
        if (saveButton != null)
            saveButton.onClick.RemoveAllListeners();
        if (quitGameButton != null)
            quitGameButton.onClick.RemoveAllListeners();

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();

        // 确保销毁时恢复游戏时间
        Time.timeScale = 1f;
    }
}
