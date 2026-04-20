using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_MainMenuView : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField]
    private Button startGameButton;
    public static string TargetScene = "StartScene";

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

    [Header("About")]
    [SerializeField]
    private GameObject aboutPanel;

    [SerializeField]
    private Button aboutCloseButton;

    [SerializeField]
    private Text aboutText;

    [Header("Settings")]
    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private Button settingsCloseButton;

    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private Button settingsSaveButton;

    [Header("Scene")]
    [SerializeField]
    private int gameSceneIndex = 1;

    [Header("Audio")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("Persistent Object")]
    [SerializeField]
    private GameObject persistentObject;

    private float tempMasterVolume;

    private void Awake()
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }

        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void Start()
    {
        BindButtonEvents();
        InitializeUI();
        PlayMenuMusic();
    }

    private void BindButtonEvents()
    {
        startGameButton.onClick.AddListener(OnStartGameClicked);
        aboutButton.onClick.AddListener(OnAboutClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        aboutCloseButton.onClick.AddListener(() => { aboutPanel.SetActive(false); });
        settingsCloseButton.onClick.AddListener(OnSettingsClose);
        settingsSaveButton.onClick.AddListener(OnSettingsSave);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
    }

    private void InitializeUI()
    {
        if (aboutText != null)
        {
            aboutText.text =
                "关于我们\n\n"
                + "游戏名称: [你的游戏名称]\n"
                + "版本: 1.0.0\n"
                + "开发团队: [你的团队名称]\n"
                + "版权所有 © 2023\n\n"
                + "感谢您的游玩。";
        }

        LoadVolumeSettings();
    }

    private void PlayMenuMusic()
    {
        if (audioManager != null)
        {
            audioManager.PlayMenuMusic();
        }
    }

    public void OnStartGameClicked()
    {
        PlayButtonClickSound();

        if (persistentObject != null)
        {
            DontDestroyOnLoad(persistentObject);
            Debug.Log($"[UI_MainMenuView] Marked {persistentObject.name} as persistent.");
        }

        StartCoroutine(LoadSceneAsync(TargetScene));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
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
        SaveCurrentVolumeToTemp();
        settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        PlayButtonClickSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SaveCurrentVolumeToTemp()
    {
        tempMasterVolume = AudioRuntimeSettings.MasterVolume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = tempMasterVolume;
        }
    }

    private void RestoreVolumeFromTemp()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = tempMasterVolume;
        }

        if (audioManager != null)
        {
            audioManager.SetMasterVolume(tempMasterVolume);
        }
    }

    private void OnSettingsClose()
    {
        PlayButtonClickSound();
        RestoreVolumeFromTemp();
        settingsPanel.SetActive(false);
    }

    private void OnSettingsSave()
    {
        PlayButtonClickSound();
        SaveVolumeSettings();
        settingsPanel.SetActive(false);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(value);
        }
    }

    private void LoadVolumeSettings()
    {
        float masterVolume = AudioRuntimeSettings.MasterVolume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVolume;
        }

        if (audioManager != null)
        {
            audioManager.SetMasterVolume(masterVolume);
        }
    }

    private void SaveVolumeSettings()
    {
        float masterVolume = masterVolumeSlider != null ? masterVolumeSlider.value : 0.5f;
        AudioRuntimeSettings.MasterVolume = masterVolume;

        if (audioManager != null)
        {
            audioManager.SetMasterVolume(masterVolume);
        }
    }

    private void PlayButtonClickSound()
    {
        if (audioManager != null)
        {
            Debug.Log("[UI_MainMenuView] Button clicked.");
        }
    }

    public void ShowMainMenu()
    {
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void HideMainMenu()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
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
    }
}
