using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spirit管理面板，用于显示玩家拥有的和已出场的Spirit
/// </summary>
public class UI_SpiritPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField]
    private GameObject panelRoot; // 面板根对象

    [Header("Deployed Spirits Section")]
    [SerializeField]
    private Transform deployedSlotsContainer; // 已出场Spirit槽位容器

    [SerializeField]
    private GameObject deployedSlotPrefab; // 已出场槽位预制体

    [Header("Owned Spirits Section")]
    [SerializeField]
    private Transform ownedSlotsContainer; // 拥有Spirit槽位容器

    [SerializeField]
    private GameObject ownedSlotPrefab; // 拥有槽位预制体

    [Header("UI Buttons")]
    [SerializeField]
    private Button closeButton; // 关闭按钮

    [Header("Spirit Detail Panel")]
    [SerializeField]
    private UI_SpiritDetailPanel detailPanel; // Spirit详情显示面板

    // 槽位数组
    private SpiritDeployedSlot[] deployedSlots;
    private SpiritOwnedSlot[] ownedSlots;

    // 选中状态
    private int selectedDeployedIndex = -1;
    private int selectedOwnedIndex = -1;

    // 面板关闭标志（防止关闭时响应点击）
    private bool isClosing = false;

    // PlayerData引用（用于初始配置）
    private PlayerData playerData;
    // Player运行时实例引用（用于动态部署/撤回操作）
    private Player player;

    private void Awake()
    {
        // 设置关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }

        // 初始时隐藏面板
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// 初始化面板（绑定PlayerData）
    /// </summary>
    public void Initialize(PlayerData data)
    {
        playerData = data;
        InitializeSlots();
    }

    /// <summary>
    /// 绑定PlayerData并刷新显示
    /// </summary>
    public void Bind(PlayerData data)
    {
        playerData = data;
        RefreshAllSlots();
    }

    /// <summary>
    /// 绑定Player运行时实例（用于动态部署/撤回功能）
    /// </summary>
    public void BindPlayer(Player playerInstance, PlayerData data = null)
    {
        Debug.Log("[UI_SpiritPanel] ===== BindPlayer called =====");
        Debug.Log($"[UI_SpiritPanel] Player instance is null: {playerInstance == null}");
        Debug.Log($"[UI_SpiritPanel] PlayerData is null: {data == null}");

        // 取消旧订阅
        if (player != null)
        {
            player.OnDataChanged -= OnPlayerDataChanged;
            Debug.Log("[UI_SpiritPanel] Unsubscribed from old Player.OnDataChanged");
        }

        // 直接使用传入的 player 和 playerData
        player = playerInstance;
        if (data != null)
        {
            playerData = data;
            Debug.Log($"[UI_SpiritPanel] 保存 PlayerData 引用: {data.name}");
        }

        Debug.Log($"[UI_SpiritPanel] 使用传入的 Player 实例");

        if (player != null)
        {
            Debug.Log($"[UI_SpiritPanel] Player.GetAllSpirits() count: {player.GetAllSpirits().Count}");
            Debug.Log($"[UI_SpiritPanel] Player.GetDeployedSpirits() count: {player.GetDeployedSpirits().Count}");
        }

        // 订阅数据变化事件
        if (player != null)
        {
            player.OnDataChanged += OnPlayerDataChanged;
            Debug.Log("[UI_SpiritPanel] Subscribed to Player.OnDataChanged");
        }

        Debug.Log($"[UI_SpiritPanel] deployedSlotsContainer is null: {deployedSlotsContainer == null}");
        Debug.Log($"[UI_SpiritPanel] ownedSlotsContainer is null: {ownedSlotsContainer == null}");

        // 重新初始化槽位
        Debug.Log("[UI_SpiritPanel] Calling InitializeSlots()...");
        InitializeSlots();
        Debug.Log("[UI_SpiritPanel] InitializeSlots() completed");

        Debug.Log($"[UI_SpiritPanel] ===== BindPlayer finished =====");
    }

    /// <summary>
    /// Player数据变化时的回调
    /// </summary>
    private void OnPlayerDataChanged()
    {
        Debug.Log("[UI_SpiritPanel] Player data changed, refreshing slots");
        RefreshAllSlots();
    }

    /// <summary>
    /// 初始化所有槽位
    /// </summary>
    private void InitializeSlots()
    {
        if (player == null)
        {
            Debug.LogWarning("[UI_SpiritPanel] Player is null, cannot initialize slots");
            return;
        }

        InitializeDeployedSlots();
        InitializeOwnedSlots();
    }

    /// <summary>
    /// 初始化已出场Spirit槽位（最多6个）
    /// </summary>
    private void InitializeDeployedSlots()
    {
        if (deployedSlotsContainer == null)
        {
            Debug.LogWarning("[UI_SpiritPanel] deployedSlotsContainer is null");
            return;
        }

        // 清除现有槽位
        foreach (Transform child in deployedSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        // 创建6个槽位
        deployedSlots = new SpiritDeployedSlot[6];
        var deployedSpirits = player != null ? player.GetDeployedSpirits() : new List<SpiritData>();

        for (int i = 0; i < 6; i++)
        {
            GameObject slotObj;

            // 使用预制体或创建默认槽位
            if (deployedSlotPrefab != null)
            {
                slotObj = Instantiate(deployedSlotPrefab, deployedSlotsContainer);
            }
            else
            {
                slotObj = CreateDefaultSlot("DeployedSlot");
                slotObj.transform.SetParent(deployedSlotsContainer, false);
            }

            slotObj.SetActive(true);

            var slot = slotObj.GetComponent<SpiritDeployedSlot>();
            if (slot == null)
                slot = slotObj.AddComponent<SpiritDeployedSlot>();

            // 获取已出场Spirit数据
            SpiritData spiritData = null;
            if (i < deployedSpirits.Count)
            {
                spiritData = deployedSpirits[i];
            }

            slot.Initialize(i, spiritData, OnDeployedSlotClicked);
            deployedSlots[i] = slot;
        }

        Debug.Log($"[UI_SpiritPanel] Initialized {deployedSlots.Length} deployed slots with {deployedSpirits.Count} spirits");
    }

    /// <summary>
    /// 初始化拥有Spirit槽位
    /// </summary>
    private void InitializeOwnedSlots()
    {
        if (ownedSlotsContainer == null)
        {
            Debug.LogWarning("[UI_SpiritPanel] ownedSlotsContainer is null");
            return;
        }

        // 清除现有槽位
        foreach (Transform child in ownedSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        // 获取拥有的Spirit列表
        var ownedSpirits = player != null ? player.GetAllSpirits() : new List<SpiritData>();
        int ownedCount = ownedSpirits.Count;
        ownedSlots = new SpiritOwnedSlot[ownedCount];

        for (int i = 0; i < ownedCount; i++)
        {
            GameObject slotObj;

            // 使用预制体或创建默认槽位
            if (ownedSlotPrefab != null)
            {
                slotObj = Instantiate(ownedSlotPrefab, ownedSlotsContainer);
            }
            else
            {
                slotObj = CreateDefaultSlot("OwnedSlot");
                slotObj.transform.SetParent(ownedSlotsContainer, false);
            }

            slotObj.SetActive(true);

            var slot = slotObj.GetComponent<SpiritOwnedSlot>();
            if (slot == null)
                slot = slotObj.AddComponent<SpiritOwnedSlot>();

            // 获取拥有Spirit数据
            SpiritData spiritData = ownedSpirits[i];

            // 检查该Spirit是否已出场
            bool isDeployed = player != null && player.IsDeployed(spiritData);

            slot.Initialize(i, spiritData, OnOwnedSlotClicked);
            slot.SetDeployed(isDeployed);
            ownedSlots[i] = slot;
        }

        Debug.Log($"[UI_SpiritPanel] Initialized {ownedSlots.Length} owned slots");
    }

    /// <summary>
    /// 创建默认槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultSlot(string name)
    {
        GameObject slotObj = new GameObject(name);

        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        slotObj.AddComponent<Button>();

        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 120);

        return slotObj;
    }

    /// <summary>
    /// 刷新所有槽位显示
    /// </summary>
    public void RefreshAllSlots()
    {
        RefreshDeployedSlots();
        RefreshOwnedSlots();
    }

    /// <summary>
    /// 刷新已出场槽位
    /// </summary>
    public void RefreshDeployedSlots()
    {
        if (deployedSlots == null || player == null)
            return;

        var deployedSpirits = player.GetDeployedSpirits();

        for (int i = 0; i < deployedSlots.Length; i++)
        {
            if (deployedSlots[i] != null)
            {
                // 更新槽位数据
                SpiritData spiritData = i < deployedSpirits.Count ? deployedSpirits[i] : null;

                // 如果数据变化了，重新初始化槽位
                if (deployedSlots[i].GetSpiritData() != spiritData)
                {
                    deployedSlots[i].Initialize(i, spiritData, OnDeployedSlotClicked);
                }

                deployedSlots[i].SetSelected(i == selectedDeployedIndex);
                deployedSlots[i].UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// 刷新拥有槽位
    /// </summary>
    public void RefreshOwnedSlots()
    {
        if (player == null)
            return;

        // 获取当前拥有的精灵列表
        var ownedSpirits = player.GetAllSpirits();
        int currentSpiritCount = ownedSpirits.Count;
        int currentSlotCount = ownedSlots != null ? ownedSlots.Length : 0;

        // 如果精灵数量发生变化，需要重新初始化槽位
        if (currentSlotCount != currentSpiritCount)
        {
            Debug.Log($"[UI_SpiritPanel] RefreshOwnedSlots: 精灵数量变化 ({currentSlotCount} -> {currentSpiritCount})，重新初始化槽位");
            InitializeOwnedSlots();
            return;
        }

        if (ownedSlots == null)
            return;

        Debug.Log($"[UI_SpiritPanel] RefreshOwnedSlots: 开始刷新 {ownedSlots.Length} 个槽位");

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] != null)
            {
                // 检查槽位数据是否与当前精灵列表匹配
                var slotSpirit = ownedSlots[i].GetSpiritData();
                var expectedSpirit = i < ownedSpirits.Count ? ownedSpirits[i] : null;

                // 如果数据不匹配，重新初始化该槽位
                if (slotSpirit != expectedSpirit)
                {
                    bool isDeployed = player.IsDeployed(expectedSpirit);
                    Debug.Log($"[UI_SpiritPanel] RefreshOwnedSlots: 槽位[{i}]数据不匹配，重新初始化（{slotSpirit?.DisplayName ?? "null"} -> {expectedSpirit?.DisplayName ?? "null"}），部署状态={isDeployed}");
                    ownedSlots[i].Initialize(i, expectedSpirit, OnOwnedSlotClicked);
                    ownedSlots[i].SetDeployed(isDeployed);
                }
                else
                {
                    // 更新部署状态
                    bool isDeployed = player.IsDeployed(slotSpirit);
                    bool slotCachedState = ownedSlots[i].IsDeployed();

                    if (isDeployed != slotCachedState)
                    {
                        Debug.LogWarning($"[UI_SpiritPanel] RefreshOwnedSlots: 槽位[{i}]({slotSpirit?.DisplayName})状态不同步！实时={isDeployed}, 缓存={slotCachedState}，更新状态");
                    }

                    ownedSlots[i].SetDeployed(isDeployed);
                }

                ownedSlots[i].SetSelected(i == selectedOwnedIndex);
                ownedSlots[i].UpdateDisplay();
            }
        }

        Debug.Log($"[UI_SpiritPanel] RefreshOwnedSlots: 刷新完成");
    }

    /// <summary>
    /// 已出场槽位点击回调
    /// </summary>
    private void OnDeployedSlotClicked(int slotIndex)
    {
        // 防止面板关闭中或不可见时响应点击
        if (isClosing || !IsVisible())
        {
            Debug.Log($"[UI_SpiritPanel] OnDeployedSlotClicked: 面板关闭中或不可见，忽略点击事件");
            return;
        }

        // 使用协程延迟执行，避免与关闭操作冲突
        StartCoroutine(HandleDeployedSlotClickCoroutine(slotIndex));
    }

    /// <summary>
    /// 已出场槽位点击处理协程（延迟一帧执行，避免与关闭冲突）
    /// </summary>
    private IEnumerator HandleDeployedSlotClickCoroutine(int slotIndex)
    {
        // 等待一帧
        yield return null;

        // 再次检查面板状态
        if (isClosing || !IsVisible())
        {
            Debug.Log($"[UI_SpiritPanel] HandleDeployedSlotClick: 延迟检查发现面板已关闭，取消操作");
            yield break;
        }

        if (player == null)
        {
            Debug.LogWarning("[UI_SpiritPanel] Player is null, cannot handle deployed slot click");
            yield break;
        }

        Debug.Log($"[UI_SpiritPanel] Deployed slot {slotIndex} clicked");

        // 获取该槽位的Spirit
        if (slotIndex < 0 || slotIndex >= deployedSlots.Length)
            yield break;

        var slot = deployedSlots[slotIndex];
        if (slot == null)
            yield break;

        var spiritData = slot.GetSpiritData();

        // 如果槽位为空，清空详情显示
        if (spiritData == null)
        {
            Debug.Log("[UI_SpiritPanel] Deployed slot is empty, clearing detail panel");
            if (detailPanel != null)
                detailPanel.Clear();
            yield break;
        }

        // 显示Spirit详情
        if (detailPanel != null)
        {
            detailPanel.ShowSpiritDetails(spiritData);
            Debug.Log($"[UI_SpiritPanel] Showing details for {spiritData.DisplayName}");
        }

        // 撤回Spirit - 直接操作player并同步到PlayerData
        Debug.Log($"[UI_SpiritPanel] OnDeployedSlotClicked: 准备撤回 {spiritData.DisplayName}，槽位索引={slotIndex}");
        bool success = player.RecallSpirit(spiritData);

        if (success)
        {
            Debug.Log($"[UI_SpiritPanel] Spirit {spiritData.DisplayName} recalled from deployment");

            // 同步到PlayerData
            if (playerData != null)
            {
                playerData.DeployedSpirits = player.GetDeployedSpirits().ToArray();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                Debug.Log($"[UI_SpiritPanel] 已同步到 PlayerData，部署数量: {playerData.DeployedSpirits.Length}");
            }

            // 不需要手动刷新，Player.OnDataChanged 事件会触发自动刷新
        }
        else
        {
            Debug.LogWarning($"[UI_SpiritPanel] Failed to recall Spirit {spiritData.DisplayName}");
        }
    }

    /// <summary>
    /// 拥有槽位点击回调
    /// </summary>
    private void OnOwnedSlotClicked(int slotIndex)
    {
        // 防止面板关闭中或不可见时响应点击
        if (isClosing || !IsVisible())
        {
            Debug.Log($"[UI_SpiritPanel] OnOwnedSlotClicked: 面板关闭中或不可见，忽略点击事件");
            return;
        }

        // 使用协程延迟执行，避免与关闭操作冲突
        StartCoroutine(HandleOwnedSlotClickCoroutine(slotIndex));
    }

    /// <summary>
    /// 拥有槽位点击处理协程（延迟一帧执行，避免与关闭冲突）
    /// </summary>
    private IEnumerator HandleOwnedSlotClickCoroutine(int slotIndex)
    {
        // 等待一帧
        yield return null;

        // 再次检查面板状态
        if (isClosing || !IsVisible())
        {
            Debug.Log($"[UI_SpiritPanel] HandleOwnedSlotClick: 延迟检查发现面板已关闭，取消操作");
            yield break;
        }

        if (player == null)
        {
            Debug.LogWarning("[UI_SpiritPanel] Player is null, cannot handle owned slot click");
            yield break;
        }

        Debug.Log($"[UI_SpiritPanel] Owned slot {slotIndex} clicked");

        // 获取该槽位的Spirit
        if (slotIndex < 0 || slotIndex >= ownedSlots.Length)
            yield break;

        var slot = ownedSlots[slotIndex];
        if (slot == null)
            yield break;

        var spiritData = slot.GetSpiritData();
        if (spiritData == null)
            yield break;

        // 显示Spirit详情
        if (detailPanel != null)
        {
            detailPanel.ShowSpiritDetails(spiritData);
            Debug.Log($"[UI_SpiritPanel] Showing details for {spiritData.DisplayName}");
        }

        // 【重要】实时从Player查询部署状态，而不是使用槽位缓存的状态
        // 这样可以避免槽位状态更新延迟导致的误判
        bool isDeployed = player != null && player.IsDeployed(spiritData);
        bool slotCachedState = slot.IsDeployed();

        Debug.Log($"[UI_SpiritPanel] OnOwnedSlotClicked: spirit={spiritData.DisplayName}, 槽位索引={slotIndex}, 实时状态={isDeployed}, 槽位缓存状态={slotCachedState}");

        // 如果状态不一致，说明槽位缓存过期，先更新槽位状态
        if (isDeployed != slotCachedState)
        {
            Debug.LogWarning($"[UI_SpiritPanel] 槽位状态不同步！实时={isDeployed}, 缓存={slotCachedState}，更新槽位状态");
            slot.SetDeployed(isDeployed);
        }

        bool success = false;
        if (isDeployed)
        {
            // 撤回Spirit - 直接操作player并同步到PlayerData
            Debug.Log($"[UI_SpiritPanel] OnOwnedSlotClicked: 准备撤回 {spiritData.DisplayName}（从拥有槽位）");
            success = player.RecallSpirit(spiritData);

            if (success)
            {
                Debug.Log($"[UI_SpiritPanel] Spirit {spiritData.DisplayName} recalled from owned slot");

                // 同步到PlayerData
                if (playerData != null)
                {
                    playerData.DeployedSpirits = player.GetDeployedSpirits().ToArray();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                    Debug.Log($"[UI_SpiritPanel] 已同步到 PlayerData，部署数量: {playerData.DeployedSpirits.Length}");
                }
            }
            else
            {
                Debug.LogWarning($"[UI_SpiritPanel] Failed to recall Spirit {spiritData.DisplayName}");
            }
        }
        else
        {
            // 部署Spirit - 直接操作player并同步到PlayerData
            Debug.Log($"[UI_SpiritPanel] OnOwnedSlotClicked: 准备部署 {spiritData.DisplayName}");
            success = player.DeploySpirit(spiritData);

            if (success)
            {
                Debug.Log($"[UI_SpiritPanel] Spirit {spiritData.DisplayName} deployed");

                // 同步到PlayerData
                if (playerData != null)
                {
                    playerData.DeployedSpirits = player.GetDeployedSpirits().ToArray();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(playerData);
#endif
                    Debug.Log($"[UI_SpiritPanel] 已同步到 PlayerData，部署数量: {playerData.DeployedSpirits.Length}");
                }
            }
            else
            {
                // 可能是因为出场槽位已满
                int remainingSlots = player.GetRemainingDeploySlots();
                Debug.LogWarning($"[UI_SpiritPanel] Failed to deploy Spirit {spiritData.DisplayName}. Remaining slots: {remainingSlots}");
            }
        }

        // 不需要手动刷新，Player.OnDataChanged 事件会触发自动刷新
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowPanel()
    {
        Debug.Log("[UI_SpiritPanel] ===== ShowPanel called =====");

        // 重置关闭标志
        isClosing = false;

        Debug.Log($"[UI_SpiritPanel] panelRoot is null: {panelRoot == null}");
        Debug.Log($"[UI_SpiritPanel] player is null: {player == null}");
        Debug.Log($"[UI_SpiritPanel] deployedSlots count: {(deployedSlots != null ? deployedSlots.Length : 0)}");
        Debug.Log($"[UI_SpiritPanel] ownedSlots count: {(ownedSlots != null ? ownedSlots.Length : 0)}");
        
        if (player != null)
        {
            Debug.Log($"[UI_SpiritPanel] player.GetAllSpirits() count: {player.GetAllSpirits().Count}");
        }

        if (panelRoot != null)
        {
            bool wasActive = panelRoot.activeSelf;
            panelRoot.SetActive(true);
            Debug.Log($"[UI_SpiritPanel] panelRoot set active (was: {wasActive}, now: true)");

            RefreshAllSlots();
            Debug.Log("[UI_SpiritPanel] Panel shown and refreshed");
        }
        else
        {
            Debug.LogError("[UI_SpiritPanel] panelRoot is null! Cannot show panel!");
        }

        Debug.Log("[UI_SpiritPanel] ===== ShowPanel finished =====");
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        Debug.Log("[UI_SpiritPanel] ===== HidePanel called =====");

        // 设置关闭标志，防止关闭过程中响应点击
        isClosing = true;

        // 停止所有点击处理协程，确保不会有延迟操作
        StopAllCoroutines();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Debug.Log("[UI_SpiritPanel] Panel hidden");
        }

        // 清空详情面板
        if (detailPanel != null)
        {
            detailPanel.Clear();
        }

        // 重置选中状态
        selectedDeployedIndex = -1;
        selectedOwnedIndex = -1;

        Debug.Log("[UI_SpiritPanel] ===== HidePanel finished =====");
    }

    /// <summary>
    /// 检查面板是否可见
    /// </summary>
    public bool IsVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (panelRoot != null)
        {
            bool isActive = !panelRoot.activeSelf;
            if (isActive)
            {
                ShowPanel();
            }
            else
            {
                HidePanel();
            }
        }
    }

    /// <summary>
    /// 获取当前选中的已出场Spirit数据
    /// </summary>
    public SpiritData GetSelectedDeployedSpirit()
    {
        if (selectedDeployedIndex >= 0 && selectedDeployedIndex < deployedSlots.Length)
        {
            return deployedSlots[selectedDeployedIndex]?.GetSpiritData();
        }
        return null;
    }

    /// <summary>
    /// 获取当前选中的拥有Spirit数据
    /// </summary>
    public SpiritData GetSelectedOwnedSpirit()
    {
        if (selectedOwnedIndex >= 0 && selectedOwnedIndex < ownedSlots.Length)
        {
            return ownedSlots[selectedOwnedIndex]?.GetSpiritData();
        }
        return null;
    }

    private void OnDestroy()
    {
        // 取消事件订阅，防止内存泄漏
        if (player != null)
        {
            player.OnDataChanged -= OnPlayerDataChanged;
        }
    }
}
