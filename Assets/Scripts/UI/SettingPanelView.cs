using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-game settings panel.
/// </summary>
public class SettingPanelView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject settingsPanel;

    [Header("Volume")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [Header("Buttons")]
    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button quitGameButton;

    [Header("Audio")]
    [SerializeField]
    private AudioManager audioManager;

    [Header("Quit")]
    [Tooltip("Leave empty to quit application directly.")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [Tooltip("If false, quit the application instead of returning to the main menu.")]
    [SerializeField]
    private bool returnToMainMenu = true;

    private bool isPanelOpen;
    private float tempMasterVolume;

    private void Awake()
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("[SettingPanelView] AudioManager not found.");
            }
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }
        else
        {
            Debug.LogError("[SettingPanelView] settingsPanel is not assigned.");
        }

        LoadVolumeSettings();
    }

    private void Start()
    {
        BindButtonEvents();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    private void BindButtonEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveAllListeners();
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
    }

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

    public void ShowPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        SaveCurrentVolumeToTemp();
        settingsPanel.SetActive(true);
        isPanelOpen = true;
        Time.timeScale = 0f;
    }

    public void HidePanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsPanel.SetActive(false);
        isPanelOpen = false;
        Time.timeScale = 1f;
    }

    public bool IsOpen()
    {
        return isPanelOpen;
    }

    private void OnCloseClicked()
    {
        PlayButtonClickSound();
        RestoreVolumeFromTemp();
        HidePanel();
    }

    private void OnSaveClicked()
    {
        PlayButtonClickSound();
        SaveVolumeSettings();
        HidePanel();
    }

    private void OnQuitGameClicked()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f;

        if (returnToMainMenu && !string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(value);
        }
    }

    private void SaveCurrentVolumeToTemp()
    {
        tempMasterVolume = masterVolumeSlider != null
            ? masterVolumeSlider.value
            : AudioRuntimeSettings.MasterVolume;
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
            Debug.Log("[SettingPanelView] Button clicked.");
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
        if (saveButton != null)
            saveButton.onClick.RemoveAllListeners();
        if (quitGameButton != null)
            quitGameButton.onClick.RemoveAllListeners();

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();

        Time.timeScale = 1f;
    }
}
