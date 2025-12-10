using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// 背包UI控制器
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject inventoryPanel; // 背包面板
    public Transform slotsContainer; // 槽位容器
    public GameObject slotPrefab; // 槽位预制体

    [Header("物品信息面板")]
    public GameObject itemInfoPanel; // 物品信息面板
    public Text itemNameText; // 物品名称
    public Text itemDescriptionText; // 物品描述
    public Text itemStatsText; // 物品属性
    public Button useButton; // 使用按钮
    public Button dropButton; // 丢弃按钮

    [Header("拖拽相关")]
    public GameObject dragItemIcon; // 拖拽时的图标
    public CanvasGroup dragCanvasGroup; // 拖拽画布组

    private List<InventorySlot> slots = new List<InventorySlot>();
    private InventorySlot draggedSlot; // 正在拖拽的槽位
    private InventorySlot selectedSlot; // 选中的槽位

    // ========== 反射辅助（兼容不同实现的 UIManagerService） ==========
    private object uiServiceInstance => UIManagerService.Instance as object;

    private GameObject TryGetServicePanel(string panelName)
    {
        var ui = uiServiceInstance;
        if (ui == null || string.IsNullOrEmpty(panelName)) return null;
        var t = ui.GetType();
        var m = t.GetMethod("GetPanel", BindingFlags.Public | BindingFlags.Instance);
        if (m == null) return null;
        try { return m.Invoke(ui, new object[] { panelName }) as GameObject; } catch { return null; }
    }

    private bool TryRegisterServicePanel(string panelName, GameObject panelObj)
    {
        var ui = uiServiceInstance;
        if (ui == null || string.IsNullOrEmpty(panelName) || panelObj == null) return false;
        var t = ui.GetType();
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var m in methods)
        {
            if (m.Name != "RegisterPanel") continue;
            var ps = m.GetParameters();
            if (ps.Length == 2 &&
                ps[0].ParameterType == typeof(string) &&
                typeof(GameObject).IsAssignableFrom(ps[1].ParameterType))
            {
                try { m.Invoke(ui, new object[] { panelName, panelObj }); return true; } catch { return false; }
            }
        }
        return false;
    }

    private bool TryIsPanelActive(string panelName, out bool isActive)
    {
        isActive = false;
        var ui = uiServiceInstance;
        if (ui == null || string.IsNullOrEmpty(panelName)) return false;
        var t = ui.GetType();
        var m = t.GetMethod("IsPanelActive", BindingFlags.Public | BindingFlags.Instance);
        if (m == null) return false;
        try
        {
            var res = m.Invoke(ui, new object[] { panelName });
            if (res is bool b) { isActive = b; return true; }
        }
        catch { }
        return false;
    }

    private void TryShowPanel(string panelName)
    {
        var ui = uiServiceInstance;
        if (ui == null || string.IsNullOrEmpty(panelName)) return;
        var t = ui.GetType();
        var m = t.GetMethod("ShowPanel", BindingFlags.Public | BindingFlags.Instance);
        if (m == null) return;
        try { m.Invoke(ui, new object[] { panelName }); } catch { }
    }

    private void TryHidePanel(string panelName)
    {
        var ui = uiServiceInstance;
        if (ui == null || string.IsNullOrEmpty(panelName)) return;
        var t = ui.GetType();
        var m = t.GetMethod("HidePanel", BindingFlags.Public | BindingFlags.Instance);
        if (m == null) return;
        try { m.Invoke(ui, new object[] { panelName }); } catch { }
    }
    // ================================================================

    private void Start()
    {
        InitializeUI();
<<<<<<< Updated upstream
        SubscribeToEvents();
=======
        // 订阅移至 OnEnable，避免初始未激活导致的丢失

        // 尝试自动向 UIManagerService 注册面板，避免未找到面板的日志
        if (inventoryPanel != null && UIManagerService.Instance != null)
        {
            // 使用反射安全注册（兼容无该重载的旧版服务）
            TryRegisterServicePanel("InventoryPanel", inventoryPanel);
        }
>>>>>>> Stashed changes

        // 初始隐藏拖拽图标
        if (dragItemIcon != null)
            dragItemIcon.SetActive(false);

        // 隐藏物品信息面板
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 创建槽位
        for (int i = 0; i < InventoryManager.Instance.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            slot.slotIndex = i;

            // 订阅槽位事件
            slot.OnSlotClicked += OnSlotClicked;
            slot.OnSlotBeginDrag += OnSlotBeginDrag;
            slot.OnSlotEndDrag += OnSlotEndDrag;
            slot.OnSlotDrop += OnSlotDrop;

            slots.Add(slot);
        }

        // 更新背包显示
        UpdateInventoryUI();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅背包变化事件
        InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;

        // 按钮事件
        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonClicked);

        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropButtonClicked);
    }

    /// <summary>
    /// 更新背包UI
    /// </summary>
    private void UpdateInventoryUI()
    {
        // 清空所有槽位
        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }

        // 更新有物品的槽位
        for (int i = 0; i < InventoryManager.Instance.items.Count; i++)
        {
            if (i < slots.Count)
            {
                InventoryItem item = InventoryManager.Instance.items[i];
                slots[i].UpdateSlot(item);
            }
        }
    }

    #region 背包操作

    /// <summary>
    /// 打开/关闭背包
    /// </summary>
    public void ToggleInventory()
    {
        bool isActive = UIManagerService.Instance.IsPanelActive("InventoryPanel");
        if (isActive)
        {
<<<<<<< Updated upstream
            UIManagerService.Instance.HidePanel("InventoryPanel");
=======
            try
            {
                GameObject svcPanel = TryGetServicePanel("InventoryPanel");

                if (svcPanel == null && inventoryPanel != null)
                {
                    TryRegisterServicePanel("InventoryPanel", inventoryPanel);
                    svcPanel = TryGetServicePanel("InventoryPanel") ?? inventoryPanel;
                }

                if (svcPanel != null)
                {
                    handledByService = true;
                    bool isActive;
                    if (TryIsPanelActive("InventoryPanel", out isActive))
                    {
                        if (isActive) TryHidePanel("InventoryPanel");
                        else { TryShowPanel("InventoryPanel"); UpdateInventoryUI(); }
                    }
                    else
                    {
                        // 回退：直接检查对象的 activeSelf
                        bool svcActive = svcPanel.activeSelf;
                        if (svcActive) TryHidePanel("InventoryPanel"); else { TryShowPanel("InventoryPanel"); UpdateInventoryUI(); }
                    }
                }
            }
            catch { handledByService = false; }
>>>>>>> Stashed changes
        }
        else
        {
            UIManagerService.Instance.ShowPanel("InventoryPanel");
            UpdateInventoryUI();
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
            return;
        }

        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(true);

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
        // 更新拖拽图标位置
        if (dragItemIcon != null && dragItemIcon.activeSelf)
        {
            dragItemIcon.transform.position = Input.mousePosition;
        }

        // 快捷键：I键打开/关闭背包
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // 快捷键：R键整理背包
        if (
            Input.GetKeyDown(KeyCode.R) && UIManagerService.Instance.IsPanelActive("InventoryPanel")
        )
        {
<<<<<<< Updated upstream
            SortInventory();
=======
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
                    var p = TryGetServicePanel("InventoryPanel");
                    panelOpen = p != null && (TryIsPanelActive("InventoryPanel", out bool ia) ? ia : p.activeSelf);
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
>>>>>>> Stashed changes
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
}
