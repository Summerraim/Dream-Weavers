using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包UI控制器
/// </summary>
public class UI_InventoryView : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject inventoryPanel; // 背包面板
    public Transform slotsContainer; // 槽位容器
    public GameObject slotPrefab; // 槽位预制体
    [Tooltip("背包面板的浅色背景 Image（仅引用，不做样式配置）")]
    public Image inventoryBackground;
    [Tooltip("运行时是否默认关闭背包面板（按 I 打开）")]
    public bool startClosed = true;
    [Tooltip("展示顺序：勾选则最新添加的物品显示在前面（索引小）")]
    public bool showNewestFirst = true;

    [Header("显示控制")]
    [Tooltip("独立切换背包面板，不通过 UIManagerService（避免影响房间UI）")]
    public bool independentToggle = true;

    [Header("槽位布局")]
    [Tooltip("每个槽位之间的空隙（x=水平, y=垂直）")]
    public Vector2 slotSpacing = new Vector2(8f, 8f);

    [Header("物品信息面板")]
    public GameObject itemInfoPanel; // 物品信息面板
    public TextMeshProUGUI itemNameText; // 物品名称
    public TextMeshProUGUI itemDescriptionText; // 物品描述
    public TextMeshProUGUI itemStatsText; // 物品属性
    public Image itemInfoBg; // 物品信息界面背景图片（若为空则使用纯色背景）
    public Button useButton; // 使用按钮
    public Button dropButton; // 丢弃按钮
    
    [Header("悬停信息面板布局")]
    [Tooltip("信息面板相对于槽位底部中心的偏移（像素）")]
    public Vector2 infoPanelOffset = new Vector2(0f, -20f);
    [Tooltip("信息面板是否拦截鼠标（建议关闭以避免悬停闪烁）")]
    public bool infoPanelBlockRaycasts = false;
    [Tooltip("根据槽位左右位置自动将信息面板放在左/右侧显示")]
    public bool infoPanelAutoFlipBySide = true;
    [Tooltip("信息面板与槽位侧向间距（像素），用于左/右侧显示时的水平空隙")]
    public float infoPanelSideMargin = 16f;
    [Tooltip("为信息面板添加背景，避免文字被道具视觉遮挡")]
    // public bool infoPanelUseBackground = true;
    // [Tooltip("信息面板背景颜色（若未指定精灵则使用纯色背景）")]
    // public Color infoPanelBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    // [Tooltip("信息面板背景精灵，可选。如果为空则使用纯色背景")] 
    // public Sprite infoPanelBackgroundSprite;

    [Header("拖拽相关")]
    public GameObject dragItemIcon; // 拖拽时的图标
    public CanvasGroup dragCanvasGroup; // 拖拽画布组

    private List<InventorySlot> slots = new List<InventorySlot>();
    private InventorySlot draggedSlot; // 正在拖拽的槽位
    private InventorySlot selectedSlot; // 选中的槽位
    private InventorySlot hoveredSlot; // 悬停中的槽位
    private bool uiInitialized = false; // 防止重复初始化
    private bool subscribed = false; // 防止重复订阅

    private void Start()
    {
        InitializeUI();
        // 订阅移至 OnEnable，避免初始未激活导致的丢失

        // 尝试自动向 UIManagerService 注册面板，避免未找到面板的日志
        if (!independentToggle && inventoryPanel != null && UIManagerService.Instance != null)
        {
            try
            {
                // 如果存在 RegisterPanel(name, GameObject) 方法则调用
                UIManagerService.Instance.RegisterPanel("InventoryPanel", inventoryPanel);
            }
            catch (System.MissingMethodException)
            {
                // 兼容旧版无注册方法的服务：忽略
            }
            catch (System.Exception)
            {
                // 任何异常均忽略，继续使用本地后备逻辑
            }
        }

        // 初始隐藏拖拽图标
        if (dragItemIcon != null)
            dragItemIcon.SetActive(false);

        // 防止拖拽图标遮挡其他 UI 的交互
        var dragImg = dragItemIcon != null ? dragItemIcon.GetComponent<Image>() : null;
        if (dragImg != null) dragImg.raycastTarget = false;
        if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = false;

        // 隐藏物品信息面板
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);

        // 配置信息面板的 Raycast 拦截，避免悬停时被面板挡住造成闪烁
        if (itemInfoPanel != null)
            ConfigureInfoPanelRaycast(infoPanelBlockRaycasts);

        // 若启用背景，则确保信息面板挂有 Image 并设置背景样式
        // if (itemInfoPanel != null && infoPanelUseBackground)
        //     EnsureInfoPanelBackground();

        // Start 阶段也尝试关闭（保险）
        if (startClosed && inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // 初始隐藏背包背景，避免挡住其他 UI
        if (inventoryBackground != null)
        {
            inventoryBackground.raycastTarget = false; // 不拦截鼠标
            inventoryBackground.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 注意：如果 inventoryPanel 就是该脚本挂载的同一个 GameObject，
        // 在 OnEnable 里把它关闭会阻止 Start 执行，从而导致槽位未初始化（slots=0）。
        // 因此仅当 panel 与自身不同对象时才在这里关闭；否则留到 Start 里再关闭。
        if (startClosed && inventoryPanel != null && inventoryPanel != this.gameObject)
        {
            inventoryPanel.SetActive(false);
        }

        // 在启用时进行事件订阅（防重复）
        if (!subscribed)
        {
            SubscribeToEvents();
            subscribed = true;
            Debug.Log("[InventoryUI] OnEnable: 已订阅 InventoryManager.OnInventoryChanged");
        }
    }

    // 注意：不要在 OnDisable 取消订阅，因为切换面板显示会禁用该对象，导致订阅被移除
    // 将取消订阅的生命周期留到 OnDestroy 即可，避免打开/关闭背包时丢失订阅

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        if (uiInitialized) return;
        Debug.Log("[InventoryUI] InitializeUI 开始：maxSlots=" + (InventoryManager.Instance != null ? InventoryManager.Instance.maxSlots : -1));

        // 基础引用校验
        if (slotsContainer == null)
        {
            Debug.LogError("[InventoryUI] 初始化失败：slotsContainer 未绑定。请在 Inspector 绑定槽位容器（含 GridLayoutGroup）。");
            return;
        }
        if (slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] 初始化失败：slotPrefab 未绑定。请在 Inspector 绑定槽位预制体（挂有 InventorySlot 组件）。");
            return;
        }
        // 布局：设置为横向排列，每行最多 2 个（需要槽位容器上挂有 GridLayoutGroup）
        var grid = slotsContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.startAxis = GridLayoutGroup.Axis.Horizontal; // 横向优先
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 固定列
            grid.constraintCount = 2; // 每行 2 列
            grid.spacing = slotSpacing; // 槽位间距
        }
        // 先收集容器下已有的 InventorySlot（例如场景中的模板/静态槽位）
        slots.Clear();
        for (int c = 0; c < slotsContainer.childCount; c++)
        {
            var child = slotsContainer.GetChild(c);
            var existing = child.GetComponent<InventorySlot>();
            if (existing != null)
            {
                existing.slotIndex = slots.Count;
                existing.OnSlotClicked += OnSlotClicked;
                existing.OnSlotBeginDrag += OnSlotBeginDrag;
                existing.OnSlotEndDrag += OnSlotEndDrag;
                existing.OnSlotDrop += OnSlotDrop;
                existing.OnSlotHoverEnter += OnSlotHoverEnter;
                existing.OnSlotHoverExit += OnSlotHoverExit;
                slots.Add(existing);
            }
        }

        // 更新背包显示
        UpdateInventoryUI();
        uiInitialized = true;
        Debug.Log("[InventoryUI] InitializeUI 完成：slots=" + slots.Count);
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅背包变化事件（带空引用保护与日志）
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
            Debug.Log("[InventoryUI] SubscribeToEvents: 成功订阅 InventoryManager.OnInventoryChanged");
        }
        else
        {
            Debug.LogWarning("[InventoryUI] SubscribeToEvents: InventoryManager.Instance 为 null，暂不订阅");
        }

        // 按钮事件
        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonClicked);
        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropButtonClicked);
    }

    /// <summary>
    /// 更新背包UI
    private void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[InventoryUI] 刷新失败：InventoryManager.Instance 为 null");
            return;
        }
        Debug.Log($"[InventoryUI] 刷新背包：items={InventoryManager.Instance.items.Count}, slots(现有)={slots.Count}");
        // 槽位数量按物品数量动态生成/回收
        int count = InventoryManager.Instance.items.Count;
        EnsureSlotCount(count);
        Debug.Log($"[InventoryUI] EnsureSlotCount 后：slots(当前)={slots.Count}");

        // 更新有物品的槽位（支持最新在前）
        for (int i = 0; i < count; i++)
        {
            int srcIndex = showNewestFirst ? (count - 1 - i) : i;
            if (i < slots.Count)
            {
                InventoryItem item = InventoryManager.Instance.items[srcIndex];
                if (item == null || item.data == null)
                {
                    Debug.LogWarning($"[InventoryUI] 第{srcIndex}个物品为空或缺少数据");
                }
                else
                {
                    var hasIcon = item.data.Icon != null;
                    Debug.Log($"[InventoryUI] 槽位{i} <- 物品[{srcIndex}] '{item.data.DisplayName}', Icon={(hasIcon ? "有" : "无")}, qty={item.quantity}");
                }
                slots[i].UpdateSlot(item);
            }
            else
            {
                Debug.LogWarning($"[InventoryUI] 槽位索引越界：i={i}, slots.Count={slots.Count}");
            }
        }
    }

    /// <summary>
    /// 根据期望数量创建/销毁槽位，并维护事件与索引
    /// </summary>
    private void EnsureSlotCount(int desired)
    {
        if (slotsContainer == null || slotPrefab == null) return;

        // 多余的先移除
        for (int i = slots.Count - 1; i >= desired; i--)
        {
            var s = slots[i];
            if (s != null)
            {
                s.OnSlotClicked -= OnSlotClicked;
                s.OnSlotBeginDrag -= OnSlotBeginDrag;
                s.OnSlotEndDrag -= OnSlotEndDrag;
                s.OnSlotDrop -= OnSlotDrop;
                s.OnSlotHoverEnter -= OnSlotHoverEnter;
                s.OnSlotHoverExit -= OnSlotHoverExit;
                Destroy(s.gameObject);
            }
            slots.RemoveAt(i);
        }

        // 不足的补齐
        while (slots.Count < desired)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            if (slotObj == null)
            {
                Debug.LogError($"[InventoryUI] 预制体实例化失败：索引 {slots.Count}");
                break;
            }
            var slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.LogError($"[InventoryUI] 预制体缺少 InventorySlot 组件：索引 {slots.Count}，预制体={slotPrefab.name}");
                Destroy(slotObj);
                break;
            }
            slot.OnSlotClicked += OnSlotClicked;
            slot.OnSlotBeginDrag += OnSlotBeginDrag;
            slot.OnSlotEndDrag += OnSlotEndDrag;
            slot.OnSlotDrop += OnSlotDrop;
            slot.OnSlotHoverEnter += OnSlotHoverEnter;
            slot.OnSlotHoverExit += OnSlotHoverExit;
            slots.Add(slot);
        }

        // 重置索引，避免拖拽交换引用错误
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) slots[i].slotIndex = i;
        }
    }

    /// <summary>
    /// 槽位悬停开始：显示物品名称与描述
    /// </summary>
    private void OnSlotHoverEnter(InventorySlot slot)
    {
        hoveredSlot = slot;
        var item = slot != null ? slot.GetItem() : null;
        ShowItemInfo(item);
        // 将信息面板置于同级的最前，确保不被其他 UI 视觉遮挡
        itemInfoPanel.transform.SetAsLastSibling();
        PositionInfoPanelUnder(slot);
    }

    /// <summary>
    /// 槽位悬停结束：隐藏物品信息面板
    /// </summary>
    private void OnSlotHoverExit(InventorySlot slot)
    {
        if (hoveredSlot == slot)
            hoveredSlot = null;
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
        if (itemInfoBg != null)
            itemInfoBg.gameObject.SetActive(false);
    }

    /// <summary>
    /// 将信息面板定位到目标槽位的正下方（槽位底部中心偏移）
    /// </summary>
    private void PositionInfoPanelUnder(InventorySlot slot)
    {
        if (itemInfoPanel == null || slot == null) return;
        var panelRT = itemInfoPanel.GetComponent<RectTransform>();
        var slotRT = slot.GetComponent<RectTransform>();
        if (panelRT == null || slotRT == null) return;

        var parentRT = panelRT.parent as RectTransform;
        var canvas = panelRT.GetComponentInParent<Canvas>();
        if (parentRT == null || canvas == null) return;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 计算槽位的几个关键点（世界坐标）
        Vector3[] corners = new Vector3[4];
        slotRT.GetWorldCorners(corners); // 0:左下 1:左上 2:右上 3:右下
        Vector3 leftMidWorld = (corners[0] + corners[1]) * 0.5f;
        Vector3 rightMidWorld = (corners[2] + corners[3]) * 0.5f;
        Vector3 centerWorld = slotRT.TransformPoint(slotRT.rect.center);

        // 转换到父矩形的局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT,
            RectTransformUtility.WorldToScreenPoint(cam, leftMidWorld), cam, out var leftMidLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT,
            RectTransformUtility.WorldToScreenPoint(cam, rightMidWorld), cam, out var rightMidLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT,
            RectTransformUtility.WorldToScreenPoint(cam, centerWorld), cam, out var centerLocal);

        // 以父矩形中心为分界来判断左右
        float parentCenterX = (parentRT.rect.xMin + parentRT.rect.xMax) * 0.5f;
        bool placeRight = !infoPanelAutoFlipBySide ? true : (centerLocal.x >= parentCenterX);

        // 目标位置（锚点坐标）：
        // - 横向：根据左右选择，放到槽位的左/右边缘外侧，留出 sideMargin，并考虑面板 pivot/尺寸
        // - 纵向：以槽位中心为基准，加上传入的垂直偏移（infoPanelOffset.y）
        Vector2 target = centerLocal; // 先用中心作为基
        float w = panelRT.rect.width;
        float h = panelRT.rect.height;
        float pivotX = panelRT.pivot.x;
        float pivotY = panelRT.pivot.y;

        if (placeRight)
        {
            // 期望左边缘 = 槽位右中 + margin
            float desiredLeft = rightMidLocal.x + infoPanelSideMargin;
            target.x = desiredLeft + w * pivotX;
        }
        else
        {
            // 期望右边缘 = 槽位左中 - margin
            float desiredRight = leftMidLocal.x - infoPanelSideMargin;
            target.x = desiredRight - w * (1f - pivotX);
        }

        // 纵向位置：以槽位中心为基，加自定义垂直偏移
        target.y = centerLocal.y + infoPanelOffset.y;

        panelRT.anchoredPosition = target;
    }

    /// <summary>
    /// 统一设置信息面板及其子节点的 Raycast 拦截
    /// </summary>
    private void ConfigureInfoPanelRaycast(bool block)
    {
        if (itemInfoPanel == null) return;
        var cg = itemInfoPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = itemInfoPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = block;

        var graphics = itemInfoPanel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = block;
        }
    }

    /// <summary>
    /// 确保信息面板具备背景 Image，并应用颜色/精灵设置
    /// </summary>
    // private void EnsureInfoPanelBackground()
    // {
    //     if (itemInfoPanel == null) return;
    //     var img = itemInfoPanel.GetComponent<Image>();
    //     if (img == null) img = itemInfoPanel.AddComponent<Image>();
    //     img.sprite = infoPanelBackgroundSprite;
    //     // img.color = infoPanelBackgroundColor;
    //     // 背景不拦截鼠标，避免悬停闪烁
    //     img.raycastTarget = infoPanelBlockRaycasts;
    // }

    #region 背包操作

    /// <summary>
    /// 打开/关闭背包
    /// </summary>
    public void ToggleInventory()
    {
        // 当启用独立切换时，仅本地显示/隐藏背包面板，避免影响房间UI
        if (independentToggle)
        {
            if (inventoryPanel != null)
            {
                bool next = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(next);
                if (inventoryBackground != null)
                    inventoryBackground.gameObject.SetActive(next);
                if (next) UpdateInventoryUI();
            }
            return;
        }

        // 使用 UIManagerService 的模式（可能会独占面板显示，谨慎使用）
        bool handledByService = false;
        var ui = UIManagerService.Instance;
        if (ui != null)
        {
            try
            {
                GameObject svcPanel = null;
                try { svcPanel = ui.GetPanel("InventoryPanel"); } catch { svcPanel = null; }

                if (svcPanel == null && inventoryPanel != null)
                {
                    try { ui.RegisterPanel("InventoryPanel", inventoryPanel); svcPanel = inventoryPanel; } catch { }
                }

                if (svcPanel != null)
                {
                    handledByService = true;
                    bool isActive = ui.IsPanelActive("InventoryPanel");
                    if (isActive)
                    {
                        ui.HidePanel("InventoryPanel");
                        if (inventoryBackground != null)
                            inventoryBackground.gameObject.SetActive(false);
                    }
                    else
                    {
                        ui.ShowPanel("InventoryPanel");
                        if (inventoryBackground != null)
                            inventoryBackground.gameObject.SetActive(true);
                        UpdateInventoryUI();
                    }
                }
            }
            catch { handledByService = false; }
        }

        // 本地兜底逻辑
        if (!handledByService && inventoryPanel != null)
        {
            bool next = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(next);
            if (inventoryBackground != null)
                inventoryBackground.gameObject.SetActive(next);
            if (next) UpdateInventoryUI();
        }
    }

    /// <summary>
    /// 槽位点击事件
    /// </summary>
    private void OnSlotClicked(InventorySlot slot)
    {
        // 取消之前选中的槽位
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        // 选中当前槽位
        selectedSlot = slot;
        slot.SetSelected(true);

        // 显示物品信息
        ShowItemInfo(slot.GetItem());
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    private void OnSlotBeginDrag(InventorySlot slot)
    {
        draggedSlot = slot;
        InventoryItem item = slot.GetItem();

        if (item != null && dragItemIcon != null)
        {
            dragItemIcon.SetActive(true);
            Image icon = dragItemIcon.GetComponent<Image>();
            icon.sprite = item.data.Icon;
            icon.color = item.data.Icon != null ? Color.white : new Color(0, 0, 0, 0);

            // 跟随鼠标
            dragItemIcon.transform.position = Input.mousePosition;

            // 拖拽期间不阻挡其他 UI 的点击
            if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = false;
            var dragImg = dragItemIcon.GetComponent<Image>();
            if (dragImg != null) dragImg.raycastTarget = false;
        }
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    private void OnSlotEndDrag(InventorySlot slot)
    {
        draggedSlot = null;

        if (dragItemIcon != null)
            dragItemIcon.SetActive(false);

        // 结束拖拽恢复交互状态（仍保持不阻挡其他 UI）
        if (dragCanvasGroup != null) dragCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 物品放入槽位
    /// </summary>
    private void OnSlotDrop(InventorySlot sourceSlot, InventorySlot targetSlot)
    {
        // 交换物品位置
        if (sourceSlot != null && targetSlot != null)
        {
            int sourceIndex = sourceSlot.slotIndex;
            int targetIndex = targetSlot.slotIndex;

            InventoryManager.Instance.SwapItems(sourceIndex, targetIndex);
        }
    }

    /// <summary>
    /// 显示物品信息
    /// </summary>
    private void ShowItemInfo(InventoryItem item)
    {
        if (item == null || item.data == null)
        {
            if (itemInfoPanel != null)
                itemInfoPanel.SetActive(false);
            if (itemInfoBg != null)
                itemInfoBg.gameObject.SetActive(false);
            return;
        }

        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(true);
        if (itemInfoBg != null)
            itemInfoBg.gameObject.SetActive(true);

        // 更新UI
        itemNameText.text = item.data.DisplayName;
        itemDescriptionText.text = item.data.Description;

        // 显示物品属性（适配 IItem）
        string stats = $"可堆叠上限: {item.data.MaxStack}\n";
        stats += $"使用后消耗: {(item.data.RemoveOnUse ? "是" : "否")}\n";
        stats += $"数量: {item.quantity}/{item.data.MaxStack}";

        itemStatsText.text = stats;

        // 设置按钮状态
        useButton.interactable = item.data.CanUse(null, null);
        dropButton.interactable = true;
    }

    /// <summary>
    /// 使用按钮点击
    /// </summary>
    private void OnUseButtonClicked()
    {
        if (selectedSlot != null)
        {
            InventoryItem item = selectedSlot.GetItem();
            if (item != null)
            {
                // 直接使用选中实例，内部会处理数量与事件
                InventoryManager.Instance.UseItem(item);

                // 隐藏信息面板
                if (itemInfoPanel != null)
                    itemInfoPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 丢弃按钮点击
    /// </summary>
    private void OnDropButtonClicked()
    {
        if (selectedSlot != null)
        {
            InventoryItem item = selectedSlot.GetItem();
            if (item != null)
            {
                // 弹出确认窗口（简化版直接丢弃）
                InventoryManager.Instance.RemoveItem(item.data.ItemId, 1);

                // 隐藏信息面板
                if (itemInfoPanel != null)
                    itemInfoPanel.SetActive(false);
            }
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加物品到背包（测试用）
    /// </summary>
    public void AddRandomItem()
    {
        // 创建测试物品
        ItemData testItem = ScriptableObject.CreateInstance<ItemData>();
        testItem.ConfigureRuntime(
            "test_" + UnityEngine.Random.Range(1000, 9999),
            "测试物品" + UnityEngine.Random.Range(1, 100),
            "这是一个测试物品",
            null,
            UnityEngine.Random.Range(1, 10),
            true
        );

        InventoryManager.Instance.AddItem(testItem, UnityEngine.Random.Range(1, 5));
    }

    /// <summary>
    /// 整理背包（按类型排序）
    /// </summary>
    public void SortInventory()
    {
        // 实现排序逻辑
        // 改为按名称排序，避免旧枚举引用
        InventoryManager.Instance.items.Sort(
            (a, b) => System.StringComparer.Ordinal.Compare(a.data.DisplayName, b.data.DisplayName)
        );

        InventoryManager.Instance.OnInventoryChanged?.Invoke();
    }

    #endregion

    private void Update()
    {
        // 若尚未订阅且 InventoryManager 已就绪，进行一次补订阅
        if (!subscribed && InventoryManager.Instance != null)
        {
            SubscribeToEvents();
            subscribed = true;
            Debug.Log("[InventoryUI] Update: 检测到 InventoryManager 就绪，已补订阅 OnInventoryChanged");
        }

        // 悬停时持续更新信息面板位置，避免布局/分辨率变化导致偏移
        if (hoveredSlot != null)
        {
            PositionInfoPanelUnder(hoveredSlot);
        }

        // 更新拖拽图标位置
        if (dragItemIcon != null && dragItemIcon.activeSelf && draggedSlot != null)
        {
            dragItemIcon.transform.position = Input.mousePosition;
        }

        // 快捷键：I键打开/关闭背包
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // 快捷键：T键添加随机物品（用于快速验证事件链）
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[InventoryUI] 触发 AddRandomItem()");
            AddRandomItem();
        }

        // 快捷键：R键整理背包（仅在面板打开时，且避免未注册面板导致的警告）
        if (Input.GetKeyDown(KeyCode.R))
        {
            bool panelOpen = false;
            var ui = UIManagerService.Instance;
            if (ui != null)
            {
                try
                {
                    var p = ui.GetPanel("InventoryPanel");
                    panelOpen = p != null && ui.IsPanelActive("InventoryPanel");
                }
                catch { /* 忽略服务异常 */ }
            }
            // 若服务不可用或未注册，回退到本地判断
            if (!panelOpen && inventoryPanel != null)
            {
                panelOpen = inventoryPanel.activeSelf;
            }

            if (panelOpen)
            {
                SortInventory();
            }
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
    }

    // 背景仅做显示/隐藏控制，样式与颜色由美术在 Inspector 配置
}
