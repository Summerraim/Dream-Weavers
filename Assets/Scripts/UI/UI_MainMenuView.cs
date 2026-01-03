using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_MainMenuView : MonoBehaviour
{
    [Header("主按钮")]
    [SerializeField]
    private Button startGameButton;
    const string TargetScene = "StartScenetest";

    [SerializeField]
    private Slider ProgressBar;

    [SerializeField]
    private TMP_Text ProgressText;

    [SerializeField]
    private GameObject Loading;

    [SerializeField]
    private Button aboutButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button quitButton;

    [Header("关于面板")]
    [SerializeField]
    private GameObject aboutPanel;

    [SerializeField]
    private Button aboutCloseButton;

    [SerializeField]
    private Text aboutText;

    [Header("设置面板")]
    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private Button settingsCloseButton;

    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private Slider musicVolumeSlider;

    [SerializeField]
    private Slider sfxVolumeSlider;

    [SerializeField]
    private Button settingsSaveButton;

    [Header("场景设置")]
    [SerializeField]
    private int gameSceneIndex = 1; // 游戏场景的Build Index

    [Header("音频管理器引用")]
    [SerializeField]
    private AudioManager audioManager; // 使用新的AudioManager

    [Header("场景持久化")]
    [SerializeField]
    private GameObject persistentObject; // 切换场景时不被销毁的GameObject

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
        }

        // 初始化面板状态
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void Start()
    {
        // 绑定按钮事件
        BindButtonEvents();

        // 初始化UI状态
        InitializeUI();

        // 播放主菜单音乐（可选）
        PlayMenuMusic();
    }

    private void BindButtonEvents()
    {
        // 主按钮事件
        startGameButton.onClick.AddListener(OnStartGameClicked);
        aboutButton.onClick.AddListener(OnAboutClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // 关于面板事件
        aboutCloseButton.onClick.AddListener(() =>
        {
            Debug.Log("[UI_MainMenuView] 关闭关于面板");
            aboutPanel.SetActive(false);
        });

        // 设置面板事件
        settingsCloseButton.onClick.AddListener(OnSettingsClose);
        settingsSaveButton.onClick.AddListener(OnSettingsSave);

        // 音量滑块事件（实时预览）
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void InitializeUI()
    {
        // 初始化关于文本（你可以在这里或Inspector中设置文本）
        if (aboutText != null)
        {
            aboutText.text =
                "关于我们\n\n"
                + "游戏名称: [你的游戏名称]\n"
                + "版本: 1.0.0\n"
                + "开发团队: [你的团队名称]\n"
                + "版权所有 © 2023\n\n"
                + "感谢您的游玩！";
        }

        // 加载并应用保存的音量设置
        LoadVolumeSettings();
    }

    private void PlayMenuMusic()
    {
        if (audioManager != null)
        {
            // 调用音频管理器播放主菜单音乐
            audioManager.PlayMenuMusic();
        }
    }

    #region 主按钮功能

    public void OnStartGameClicked()
    {
        // 播放点击音效
        PlayButtonClickSound();

        // 标记GameObject在场景切换时不被销毁
        if (persistentObject != null)
        {
            DontDestroyOnLoad(persistentObject);
            Debug.Log($"[UI_MainMenuView] 已标记 {persistentObject.name} 为持久化对象");
        }

        // 可以添加加载动画或过渡效果

        // 加载游戏场景
        StartCoroutine(LoadSceneAsync(TargetScene));

        // 或者通过GameManagerService控制游戏状态
        // if (GameManagerService.Instance != null)
        // {
        //     GameManagerService.Instance.StartGame();
        // }
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        Loading.SetActive(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            ProgressBar.value = progress;
            ProgressText.text = progress.ToString("p2");
            if (progress >= 1)
            {
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private void OnAboutClicked()
    {
        PlayButtonClickSound();
        aboutPanel.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        PlayButtonClickSound();

        // 保存当前音量设置到临时变量（用于取消时恢复）
        SaveCurrentVolumeToTemp();

        // 显示设置面板
        settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        PlayButtonClickSound();

        // 确认退出对话框（可选）
        // 这里直接退出，你可以添加确认对话框

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 设置面板功能

    private void SaveCurrentVolumeToTemp()
    {
        // 保存当前音量到临时变量
        if (audioManager != null)
        {
            // 从PlayerPrefs读取保存的音量设置
            tempMasterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolumeSlider.value);
            tempMusicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolumeSlider.value);
            tempSfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolumeSlider.value);
        }
        else
        {
            tempMasterVolume = masterVolumeSlider.value;
            tempMusicVolume = musicVolumeSlider.value;
            tempSfxVolume = sfxVolumeSlider.value;
        }

        // 更新滑块显示
        masterVolumeSlider.value = tempMasterVolume;
        musicVolumeSlider.value = tempMusicVolume;
        sfxVolumeSlider.value = tempSfxVolume;
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

        Debug.Log("[UI_MainMenuView] 已恢复到保存前的音量设置");
    }

    private void OnSettingsClose()
    {
        PlayButtonClickSound();

        // 不保存，恢复之前的音量设置
        RestoreVolumeFromTemp();

        Debug.Log("[UI_MainMenuView] 关闭设置面板（未保存）");
        settingsPanel.SetActive(false);
    }

    private void OnSettingsSave()
    {
        PlayButtonClickSound();

        // 保存设置
        SaveVolumeSettings();

        // 提示保存成功（可选）
        Debug.Log("[UI_MainMenuView] 设置已保存");

        // 关闭面板
        settingsPanel.SetActive(false);
    }

    private void OnMasterVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器（仅预览，不保存）
        if (audioManager != null)
            audioManager.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器（仅预览，不保存）
        if (audioManager != null)
            audioManager.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        // 实时应用音量到音频管理器（仅预览，不保存）
        if (audioManager != null)
            audioManager.SetSFXVolume(value);
    }

    #endregion

    #region 音量设置管理

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
    }

    private void SaveVolumeSettings()
    {
        // 从滑块获取当前值
        float masterVolume = masterVolumeSlider.value;
        float musicVolume = musicVolumeSlider.value;
        float sfxVolume = sfxVolumeSlider.value;

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
    }

    #endregion

    #region 工具方法

    private void PlayButtonClickSound()
    {
        if (audioManager != null)
        {
            // 这里可以添加按钮点击音效
            // 需要先配置音效文件，然后调用 audioManager.PlaySFX(buttonClickSound);
            Debug.Log("播放按钮点击音效");
        }
    }

    #endregion

    #region 公共方法（供其他脚本调用）

    public void ShowMainMenu()
    {
        // 显示主菜单，隐藏其他面板
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // 可以添加动画效果
    }

    public void HideMainMenu()
    {
        // 隐藏主菜单
        gameObject.SetActive(false);
    }

    #endregion

    private void OnDestroy()
    {
        // 清理事件绑定，防止内存泄漏
        if (startGameButton != null)
            startGameButton.onClick.RemoveAllListeners();
        if (aboutButton != null)
            aboutButton.onClick.RemoveAllListeners();
        if (settingsButton != null)
            settingsButton.onClick.RemoveAllListeners();
        if (quitButton != null)
            quitButton.onClick.RemoveAllListeners();

        if (aboutCloseButton != null)
            aboutCloseButton.onClick.RemoveAllListeners();
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveAllListeners();
        if (settingsSaveButton != null)
            settingsSaveButton.onClick.RemoveAllListeners();

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}
