using UnityEngine;

/// <summary>
/// 玩家管理器 - 单例模式
/// 管理全局的Player运行时实例
/// </summary>
public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerManager");
                    _instance = go.AddComponent<PlayerManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("玩家数据")]
    [SerializeField] private PlayerData playerData;

    private Player player; // 运行时玩家实例

    /// <summary>
    /// 拥有精灵变更事件（捕捉/移除/部署变更时触发）
    /// </summary>
    public event System.Action OwnedSpiritsChanged;

    /// <summary>
    /// 获取当前玩家实例
    /// </summary>
    public Player CurrentPlayer => player;

    /// <summary>
    /// 获取当前PlayerData（ScriptableObject配置）
    /// </summary>
    public PlayerData CurrentPlayerData => playerData;

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

        InitializePlayer();
    }

    /// <summary>
    /// 初始化玩家
    /// </summary>
    private void InitializePlayer()
    {
        if (playerData == null)
        {
            Debug.LogWarning("[PlayerManager] PlayerData未配置，尝试从Resources查找");
            var allPlayerData = Resources.FindObjectsOfTypeAll<PlayerData>();
            if (allPlayerData.Length > 0)
            {
                playerData = allPlayerData[0];
                Debug.Log($"[PlayerManager] 找到PlayerData: {playerData.name}");
            }
            else
            {
                Debug.LogError("[PlayerManager] 未找到任何PlayerData！");
                return;
            }
        }

        player = playerData.CreatePlayer();
        Debug.Log($"[PlayerManager] Player已初始化，拥有 {player.GetAllSpirits().Count} 个精灵");
    }

    /// <summary>
    /// 设置PlayerData并重新初始化
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError("[PlayerManager] PlayerData不能为null");
            return;
        }

        playerData = data;
        InitializePlayer();
    }

    /// <summary>
    /// 捕捉精灵（添加到Player和PlayerData）
    /// </summary>
    public bool CaptureSpirit(SpiritData spirit)
    {
        if (spirit == null)
        {
            Debug.LogWarning("[PlayerManager] 尝试捕捉null的Spirit");
            return false;
        }

        bool added = false;

        // 1. 添加到运行时Player实例
        if (player != null)
        {
            added = player.AddSpirit(spirit);
            if (added)
            {
                Debug.Log($"[PlayerManager] 运行时捕捉成功: {spirit.DisplayName}");
            }
            else
            {
                Debug.Log($"[PlayerManager] 运行时已拥有: {spirit.DisplayName}");
            }
        }

        // 2. 同步到PlayerData（用于编辑器查看）
        if (playerData != null)
        {
            var owned = playerData.GetOwnedSpirits();
            if (!owned.Contains(spirit))
            {
                owned.Add(spirit);
                playerData.OwnedSpirits = owned.ToArray();

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                Debug.Log($"[PlayerManager] PlayerData已同步: {spirit.DisplayName}");
            }
        }

        if (added)
        {
            OwnedSpiritsChanged?.Invoke();
        }

        return added;
    }

    /// <summary>
    /// 获取所有拥有的精灵
    /// </summary>
    public System.Collections.Generic.List<SpiritData> GetOwnedSpirits()
    {
        return player?.GetAllSpirits() ?? new System.Collections.Generic.List<SpiritData>();
    }

    /// <summary>
    /// 按索引替换拥有的精灵，同时同步PlayerData
    /// </summary>
    public bool ReplaceOwnedSpirit(int index, SpiritData newSpirit)
    {
        if (player == null || newSpirit == null)
        {
            Debug.LogWarning("[PlayerManager] ReplaceOwnedSpirit failed: player or newSpirit null");
            return false;
        }

        var success = player.ReplaceOwnedSpirit(index, newSpirit);
        if (!success)
            return false;

        // 同步到PlayerData
        if (playerData != null)
        {
            var owned = player.GetAllSpirits();
            if (index < owned.Count)
            {
                playerData.OwnedSpirits = owned.ToArray();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(playerData);
#endif
            }
        }

        OwnedSpiritsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取所有已部署的精灵
    /// </summary>
    public System.Collections.Generic.List<SpiritData> GetDeployedSpirits()
    {
        return player?.GetDeployedSpirits() ?? new System.Collections.Generic.List<SpiritData>();
    }

    /// <summary>
    /// 部署精灵（同时更新运行时Player和PlayerData）
    /// </summary>
    public bool DeploySpirit(SpiritData spirit)
    {
        if (spirit == null)
        {
            Debug.LogWarning("[PlayerManager] 尝试部署null的Spirit");
            return false;
        }

        if (player == null)
        {
            Debug.LogError("[PlayerManager] Player未初始化");
            return false;
        }

        // 1. 部署到运行时Player实例
        bool deployed = player.DeploySpirit(spirit);

        if (deployed)
        {
            Debug.Log($"[PlayerManager] 部署成功: {spirit.DisplayName}");

            // 2. 同步到PlayerData
            SyncDeployedSpiritsToPlayerData();
        }
        else
        {
            Debug.Log($"[PlayerManager] 部署失败: {spirit.DisplayName}（可能已部署或槽位已满）");
        }

        return deployed;
    }

    /// <summary>
    /// 撤回精灵（同时更新运行时Player和PlayerData）
    /// </summary>
    public bool RecallSpirit(SpiritData spirit)
    {
        if (spirit == null)
        {
            Debug.LogWarning("[PlayerManager] 尝试撤回null的Spirit");
            return false;
        }

        if (player == null)
        {
            Debug.LogError("[PlayerManager] Player未初始化");
            return false;
        }

        // 1. 从运行时Player实例撤回
        bool recalled = player.RecallSpirit(spirit);

        if (recalled)
        {
            Debug.Log($"[PlayerManager] 撤回成功: {spirit.DisplayName}");

            // 2. 同步到PlayerData
            SyncDeployedSpiritsToPlayerData();
        }
        else
        {
            Debug.Log($"[PlayerManager] 撤回失败: {spirit.DisplayName}（可能未部署）");
        }

        return recalled;
    }

    /// <summary>
    /// 同步部署精灵列表到PlayerData
    /// </summary>
    private void SyncDeployedSpiritsToPlayerData()
    {
        if (playerData == null || player == null)
        {
            Debug.LogWarning("[PlayerManager] playerData或player为null，无法同步");
            return;
        }

        var deployedList = player.GetDeployedSpirits();
        playerData.DeployedSpirits = deployedList.ToArray();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(playerData);
#endif

        Debug.Log($"[PlayerManager] 已同步部署精灵到PlayerData，数量: {deployedList.Count}");
    }

    /// <summary>
    /// 重置玩家状态到初始状态（用于重新开始游戏）
    /// </summary>
    public void ResetToInitialState()
    {
        SpiritRuntimeSkills.ClearAll();
        Debug.Log("[PlayerManager] 重置玩家状态到初始状态");

        // 关键：重置 PlayerData（否则 ScriptableObject 上的 OwnedSpirits/DeployedSpirits 会在“重启游戏”后继续沿用上一局）
        // 逻辑对齐 PlayerData 的 [RuntimeInitializeOnLoadMethod] ResetAllPlayerData()：重置所有 PlayerData 资源。
        // 优先重置当前正在使用的 PlayerData（避免 FindObjectsOfTypeAll 在某些时机找不到导致不重置）
        if (playerData == null)
        {
            var discovered = Resources.FindObjectsOfTypeAll<PlayerData>();
            if (discovered != null && discovered.Length > 0)
            {
                playerData = discovered[0];
            }
        }

        if (playerData != null)
        {
            playerData.ResetToInitialState();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(playerData);
#endif
        }

        var allPlayerData = Resources.FindObjectsOfTypeAll<PlayerData>();
        if (allPlayerData != null && allPlayerData.Length > 0)
        {
            for (int i = 0; i < allPlayerData.Length; i++)
            {
                var pd = allPlayerData[i];
                if (pd == null) continue;
                if (pd == playerData) continue;
                pd.ResetToInitialState();

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(pd);
#endif
            }

            Debug.Log($"[PlayerManager] 已重置 {allPlayerData.Length} 个 PlayerData 资源到初始状态");
        }
        else
        {
            Debug.LogWarning("[PlayerManager] 未找到任何 PlayerData 资源，无法重置");
        }

        InitializePlayer();
        OwnedSpiritsChanged?.Invoke();
    }
}
