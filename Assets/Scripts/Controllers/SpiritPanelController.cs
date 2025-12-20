using UnityEngine;

/// <summary>
/// Spirit管理面板控制器
/// 负责管理Spirit管理面板的显示、数据绑定和交互
/// </summary>
public class SpiritPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private UI_SpiritPanel spiritPanel;

    [Header("Data References")]
    [SerializeField]
    private PlayerData playerData; // ScriptableObject配置数据

    // 运行时玩家实例
    private Player player;

    private void Awake()
    {
        if (spiritPanel == null)
        {
            spiritPanel = GetComponentInChildren<UI_SpiritPanel>(true);
        }
    }

    private void Start()
    {
        // 如果有 PlayerData，自动创建 Player 实例并初始化
        if (playerData != null && player == null)
        {
            Debug.Log("[SpiritPanelController] ===== Auto-initializing from PlayerData =====");
            Debug.Log($"[SpiritPanelController] PlayerData: {playerData.name}");
            Debug.Log($"[SpiritPanelController] PlayerData.OwnedSpirits count: {(playerData.OwnedSpirits != null ? playerData.OwnedSpirits.Length : 0)}");
            Debug.Log($"[SpiritPanelController] PlayerData.DeployedSpirits count: {(playerData.DeployedSpirits != null ? playerData.DeployedSpirits.Length : 0)}");

            // 从 PlayerData 创建 Player 实例
            player = playerData.CreatePlayer();

            if (player != null)
            {
                Debug.Log($"[SpiritPanelController] Player created successfully");
                Debug.Log($"[SpiritPanelController] Player.GetAllSpirits() count: {player.GetAllSpirits().Count}");
                Debug.Log($"[SpiritPanelController] Player.GetDeployedSpirits() count: {player.GetDeployedSpirits().Count}");

                // 打印所有Spirit
                var ownedSpirits = player.GetAllSpirits();
                for (int i = 0; i < ownedSpirits.Count; i++)
                {
                    Debug.Log($"[SpiritPanelController] Owned Spirit [{i}]: {(ownedSpirits[i] != null ? ownedSpirits[i].DisplayName : "null")}");
                }

                // 绑定到面板
                SetPlayer(player);
                Debug.Log("[SpiritPanelController] ===== Auto-initialization complete =====");
            }
            else
            {
                Debug.LogError("[SpiritPanelController] Failed to create Player from PlayerData!");
            }
        }
        else if (playerData == null)
        {
            Debug.LogWarning("[SpiritPanelController] PlayerData is not assigned! Please assign PlayerData in Inspector.");
        }
    }

    /// <summary>
    /// 设置PlayerData（ScriptableObject配置）
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;

        if (spiritPanel != null)
        {
            spiritPanel.Initialize(playerData);
        }

        Debug.Log($"[SpiritPanelController] PlayerData set: {data.PlayerName}");
    }

    /// <summary>
    /// 设置Player运行时实例（用于部署/撤回功能）
    /// </summary>
    public void SetPlayer(Player playerInstance)
    {
        Debug.Log("[SpiritPanelController] ===== SetPlayer called =====");
        Debug.Log($"[SpiritPanelController] Player instance is null: {playerInstance == null}");

        player = playerInstance;

        if (player != null)
        {
            Debug.Log($"[SpiritPanelController] Player.GetAllSpirits() count: {player.GetAllSpirits().Count}");
            Debug.Log($"[SpiritPanelController] Player.GetDeployedSpirits() count: {player.GetDeployedSpirits().Count}");
        }

        Debug.Log($"[SpiritPanelController] spiritPanel is null: {spiritPanel == null}");

        // 绑定到面板
        if (spiritPanel != null)
        {
            Debug.Log("[SpiritPanelController] Calling spiritPanel.BindPlayer()...");
            spiritPanel.BindPlayer(playerInstance);
            Debug.Log("[SpiritPanelController] spiritPanel.BindPlayer() completed");
        }
        else
        {
            Debug.LogError("[SpiritPanelController] spiritPanel is null! Cannot bind Player!");
        }

        Debug.Log("[SpiritPanelController] ===== SetPlayer finished =====");
    }

    /// <summary>
    /// 显示Spirit管理面板
    /// </summary>
    public void ShowPanel()
    {
        Debug.Log("[SpiritPanelController] ===== ShowPanel called =====");
        Debug.Log($"[SpiritPanelController] spiritPanel is null: {spiritPanel == null}");
        Debug.Log($"[SpiritPanelController] playerData is null: {playerData == null}");
        
        if (playerData != null)
        {
            Debug.Log($"[SpiritPanelController] PlayerData 实例ID: {playerData.GetInstanceID()}, 名称: {playerData.name}");
        }

        // 每次显示面板时，重新从 PlayerData 创建 Player 实例（确保获取最新数据）
        if (playerData != null)
        {
            // 打印 PlayerData 中的所有精灵
            Debug.Log($"[SpiritPanelController] === PlayerData.OwnedSpirits ({(playerData.OwnedSpirits != null ? playerData.OwnedSpirits.Length : 0)}) ===");
            if (playerData.OwnedSpirits != null)
            {
                for (int i = 0; i < playerData.OwnedSpirits.Length; i++)
                {
                    var spirit = playerData.OwnedSpirits[i];
                    Debug.Log($"[SpiritPanelController]   [{i}] {(spirit != null ? spirit.DisplayName : "null")}");
                }
            }
            
            Debug.Log($"[SpiritPanelController] === PlayerData.DeployedSpirits ({(playerData.DeployedSpirits != null ? playerData.DeployedSpirits.Length : 0)}) ===");
            if (playerData.DeployedSpirits != null)
            {
                for (int i = 0; i < playerData.DeployedSpirits.Length; i++)
                {
                    var spirit = playerData.DeployedSpirits[i];
                    Debug.Log($"[SpiritPanelController]   [{i}] {(spirit != null ? spirit.DisplayName : "null")}");
                }
            }
            
            // 重新从 PlayerData 创建 Player 实例
            player = playerData.CreatePlayer();
            Debug.Log($"[SpiritPanelController] 重新创建 Player 实例，拥有精灵: {player.GetAllSpirits().Count}, 部署精灵: {player.GetDeployedSpirits().Count}");
            
            // 重新绑定到面板（同时传递 playerData 引用）
            if (spiritPanel != null)
            {
                spiritPanel.BindPlayer(player, playerData);
            }
        }
        else
        {
            Debug.LogWarning("[SpiritPanelController] PlayerData 为 null，无法刷新精灵数据！");
        }

        if (spiritPanel != null)
        {
            Debug.Log("[SpiritPanelController] Calling spiritPanel.ShowPanel()...");
            spiritPanel.ShowPanel();
            Debug.Log("[SpiritPanelController] Showing Spirit panel");
        }
        else
        {
            Debug.LogError("[SpiritPanelController] spiritPanel is null!");
        }
    }

    /// <summary>
    /// 隐藏Spirit管理面板
    /// </summary>
    public void HidePanel()
    {
        if (spiritPanel != null)
        {
            spiritPanel.HidePanel();
            Debug.Log("[SpiritPanelController] Hiding Spirit panel");
        }
    }

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (spiritPanel != null)
        {
            // 检查面板当前状态，如果要显示则调用 ShowPanel() 以刷新数据
            bool willShow = !spiritPanel.IsVisible();
            if (willShow)
            {
                ShowPanel(); // 调用 ShowPanel 以重新加载 PlayerData 数据
            }
            else
            {
                HidePanel();
            }
        }
    }

    /// <summary>
    /// 刷新面板显示
    /// </summary>
    public void RefreshPanel()
    {
        if (spiritPanel != null)
        {
            spiritPanel.RefreshAllSlots();
        }
    }

    /// <summary>
    /// 从Player实例更新面板数据
    /// 将Player的运行时数据同步到PlayerData以便显示
    /// </summary>
    public void UpdateFromPlayer(Player playerInstance)
    {
        if (playerInstance == null)
        {
            Debug.LogWarning("[SpiritPanelController] Player instance is null");
            return;
        }

        // 使用 SetPlayer 方法绑定Player实例
        SetPlayer(playerInstance);

        var ownedSpirits = playerInstance.GetAllSpirits();
        var deployedSpirits = playerInstance.GetDeployedSpirits();

        Debug.Log($"[SpiritPanelController] Updating panel from Player: {ownedSpirits.Count} owned, {deployedSpirits.Count} deployed");

        RefreshPanel();
    }
}
