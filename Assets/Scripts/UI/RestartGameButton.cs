using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 绑定到 Unity UI Button：点击后触发“重新开始”（走 GameManagerService.StartNewGame 的现有逻辑）。
/// 也支持在 Inspector 里把 Restart() 绑到 Button.onClick。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class RestartGameButton : MonoBehaviour
{
    public enum RestartMode
    {
        StartNewGameViaService,
        LoadSceneByBuildIndex,
        LoadSceneByName
    }

    [Header("Mode")]
    [SerializeField] private RestartMode mode = RestartMode.StartNewGameViaService;

    [Header("Fallback / Scene Loading")]
    [SerializeField] private int sceneBuildIndex = 0;
    [SerializeField] private string sceneName = string.Empty;

    [Header("Options")]
    [SerializeField] private bool resetTimeScale = true;
    [SerializeField] private bool autoBindOnAwake = true;
    [SerializeField] private bool resetPlayerDataOnRestart = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (!autoBindOnAwake || button == null)
        {
            return;
        }

        button.onClick.RemoveListener(Restart);
        button.onClick.AddListener(Restart);
    }

    public void Restart()
    {
        if (resetTimeScale)
        {
            Time.timeScale = 1f;
        }

        switch (mode)
        {
            case RestartMode.StartNewGameViaService:
                if (GameManagerService.Instance != null)
                {
                    GameManagerService.Instance.StartNewGame();
                    return;
                }

                Debug.LogWarning("[RestartGameButton] GameManagerService.Instance 为 null，降级为加载场景");
                SceneManager.LoadScene(sceneBuildIndex);
                return;

            case RestartMode.LoadSceneByBuildIndex:
                if (resetPlayerDataOnRestart)
                {
                    var playerManager = FindObjectOfType<PlayerManager>();
                    if (playerManager != null)
                    {
                        playerManager.ResetToInitialState();
                    }
                }
                SceneManager.LoadScene(sceneBuildIndex);
                return;

            case RestartMode.LoadSceneByName:
                if (resetPlayerDataOnRestart)
                {
                    var playerManager = FindObjectOfType<PlayerManager>();
                    if (playerManager != null)
                    {
                        playerManager.ResetToInitialState();
                    }
                }

                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogWarning("[RestartGameButton] sceneName 为空，改为使用 sceneBuildIndex");
                    SceneManager.LoadScene(sceneBuildIndex);
                    return;
                }

                SceneManager.LoadScene(sceneName);
                return;
        }
    }
}

