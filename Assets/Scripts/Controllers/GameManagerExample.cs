using UnityEngine;

/// <summary>
/// 游戏管理器示例
/// 展示如何集成和使用SpiritPanelController
/// </summary>
public class GameManagerExample : MonoBehaviour
{
    [Header("UI控制器")]
    [SerializeField]
    private SpiritPanelController spiritPanelController;

    [Header("玩家数据")]
    [SerializeField]
    private PlayerData playerData;

    private Player player; // 运行时玩家实例

    void Start()
    {
        InitializePlayer();
    }

    /// <summary>
    /// 初始化玩家
    /// </summary>
    private void InitializePlayer()
    {
        Debug.Log("[GameManager] ===== 开始初始化玩家 =====");

        if (playerData == null)
        {
            Debug.LogError("[GameManager] PlayerData is not assigned!");
            return;
        }

        Debug.Log($"[GameManager] PlayerData found: {playerData.name}");
        Debug.Log($"[GameManager] PlayerData.OwnedSpirits count: {(playerData.OwnedSpirits != null ? playerData.OwnedSpirits.Length : 0)}");
        Debug.Log($"[GameManager] PlayerData.DeployedSpirits count: {(playerData.DeployedSpirits != null ? playerData.DeployedSpirits.Length : 0)}");

        // 从 PlayerData 创建运行时 Player 实例
        player = playerData.CreatePlayer();

        if (player == null)
        {
            Debug.LogError("[GameManager] Failed to create Player instance!");
            return;
        }

        Debug.Log($"[GameManager] Player created successfully");
        Debug.Log($"[GameManager] Player.GetAllSpirits() count: {player.GetAllSpirits().Count}");
        Debug.Log($"[GameManager] Player.GetDeployedSpirits() count: {player.GetDeployedSpirits().Count}");

        // 打印所有拥有的Spirit
        var ownedSpirits = player.GetAllSpirits();
        for (int i = 0; i < ownedSpirits.Count; i++)
        {
            Debug.Log($"[GameManager] Owned Spirit [{i}]: {(ownedSpirits[i] != null ? ownedSpirits[i].DisplayName : "null")}");
        }

        // 打印所有已出场的Spirit
        var deployedSpirits = player.GetDeployedSpirits();
        for (int i = 0; i < deployedSpirits.Count; i++)
        {
            Debug.Log($"[GameManager] Deployed Spirit [{i}]: {(deployedSpirits[i] != null ? deployedSpirits[i].DisplayName : "null")}");
        }

        // 绑定 Player 实例到 Spirit 面板
        if (spiritPanelController != null)
        {
            Debug.Log("[GameManager] Binding Player to SpiritPanelController...");
            spiritPanelController.SetPlayer(player);
            Debug.Log("[GameManager] SpiritPanel bound to Player");
        }
        else
        {
            Debug.LogError("[GameManager] SpiritPanelController is not assigned!");
        }

        Debug.Log("[GameManager] ===== 玩家初始化完成 =====");
    }

    /// <summary>
    /// 公开方法：打开Spirit管理面板
    /// 可以从其他脚本或UI按钮调用
    /// </summary>
    public void OpenSpiritPanel()
    {
        if (spiritPanelController != null)
        {
            spiritPanelController.ShowPanel();
        }
    }

    /// <summary>
    /// 公开方法：关闭Spirit管理面板
    /// </summary>
    public void CloseSpiritPanel()
    {
        if (spiritPanelController != null)
        {
            spiritPanelController.HidePanel();
        }
    }

    /// <summary>
    /// 公开方法：切换Spirit管理面板显示状态
    /// </summary>
    public void ToggleSpiritPanel()
    {
        if (spiritPanelController != null)
        {
            spiritPanelController.TogglePanel();
        }
    }

    void Update()
    {
        // 示例：按空格键打开Spirit面板
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[GameManager] ===== Space键按下 =====");
            Debug.Log($"[GameManager] Player is null: {player == null}");
            Debug.Log($"[GameManager] SpiritPanelController is null: {spiritPanelController == null}");

            ToggleSpiritPanel();
        }

        // 示例：按ESC键关闭Spirit面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[GameManager] ESC键按下，关闭面板");
            if (spiritPanelController != null)
            {
                CloseSpiritPanel();
            }
        }
    }
}
