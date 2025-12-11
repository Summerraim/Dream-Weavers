using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spirit切换器UI - 显示6个阵容槽位，允许玩家手动切换Spirit
/// </summary>
public class UI_SpiritSwitcher : MonoBehaviour
{
    [Header("Spirit Slot Prefab")]
    [SerializeField]
    private GameObject spiritSlotPrefab;

    [Header("Slots Container")]
    [SerializeField]
    private Transform slotsContainer;

    [Header("Toggle Button")]
    [SerializeField]
    private Button toggleButton;

    [Header("Panel")]
    [SerializeField]
    private GameObject switcherPanel;

    private BattleController battleController;
    private SpiritSlot[] spiritSlots;

    private void Awake()
    {
        // 初始化时隐藏面板
        if (switcherPanel != null)
            switcherPanel.SetActive(false);

        // 绑定切换按钮
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
    }

    /// <summary>
    /// 绑定BattleController并初始化槽位
    /// </summary>
    public void Bind(BattleController controller)
    {
        battleController = controller;
        InitializeSlots();
    }

    /// <summary>
    /// 初始化6个Spirit槽位
    /// </summary>
    private void InitializeSlots()
    {
        if (battleController == null || slotsContainer == null)
            return;

        // 清除现有槽位
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        // 创建6个槽位
        spiritSlots = new SpiritSlot[6];
        var deployedSpirits = battleController.GetDeployedSpirits();

        for (int i = 0; i < 6; i++)
        {
            GameObject slotObj;

            // 如果有预制体，使用预制体；否则创建简单的按钮
            if (spiritSlotPrefab != null)
            {
                slotObj = Instantiate(spiritSlotPrefab, slotsContainer);
            }
            else
            {
                slotObj = CreateDefaultSlot();
                slotObj.transform.SetParent(slotsContainer, false);
            }

            // 确保GameObject激活
            slotObj.SetActive(true);

            // 获取SpiritSlot组件
            var slot = slotObj.GetComponent<SpiritSlot>();
            if (slot == null)
                slot = slotObj.AddComponent<SpiritSlot>();

            // 激活slot组件
            slot.enabled = true;

            // 设置槽位数据
            if (i < deployedSpirits.Count && deployedSpirits[i] != null)
            {
                slot.Initialize(i, deployedSpirits[i], OnSlotClicked);
            }
            else
            {
                slot.Initialize(i, null, OnSlotClicked);
            }

            spiritSlots[i] = slot;
        }

        RefreshSlots();
    }

    /// <summary>
    /// 创建默认槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultSlot()
    {
        GameObject slotObj = new GameObject("SpiritSlot");

        // 添加Image组件作为背景
        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 添加Button组件
        slotObj.AddComponent<Button>();

        // 设置RectTransform
        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        return slotObj;
    }

    /// <summary>
    /// 刷新所有槽位的状态
    /// </summary>
    public void RefreshSlots()
    {
        if (spiritSlots == null || battleController == null)
            return;

        int currentIndex = battleController.GetCurrentSpiritIndex();

        for (int i = 0; i < spiritSlots.Length; i++)
        {
            if (spiritSlots[i] != null)
            {
                // 设置选中状态
                spiritSlots[i].SetSelected(i == currentIndex);

                // 更新HP/MP状态
                bool isAlive = battleController.IsSpiritAlive(i);
                var runtimeData = battleController.GetSpiritRuntimeData(i);

                spiritSlots[i]
                    .UpdateStatus(
                        runtimeData.CurrentHP,
                        runtimeData.MaxHP,
                        runtimeData.CurrentMP,
                        runtimeData.MaxMP,
                        isAlive
                    );
            }
        }
    }

    /// <summary>
    /// 槽位点击回调
    /// </summary>
    private void OnSlotClicked(int slotIndex)
    {
        if (battleController == null)
            return;

        Debug.Log($"UI_SpiritSwitcher: Slot {slotIndex} clicked");

        // 尝试切换到指定Spirit
        bool success = battleController.SwitchToSpirit(slotIndex);

        if (success)
        {
            RefreshSlots();
            Debug.Log($"UI_SpiritSwitcher: Successfully switched to Spirit {slotIndex}");

            // 切换成功后关闭面板
            HidePanel();

            // 触发结束回合
            battleController.EndPlayerTurn();
        }
        else
        {
            Debug.LogWarning($"UI_SpiritSwitcher: Failed to switch to Spirit {slotIndex}");
        }
    }

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (switcherPanel != null)
        {
            bool isActive = !switcherPanel.activeSelf;
            switcherPanel.SetActive(isActive);

            if (isActive)
            {
                RefreshSlots();
            }
        }
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowPanel()
    {
        if (switcherPanel != null)
        {
            switcherPanel.SetActive(true);
            RefreshSlots();
        }
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        if (switcherPanel != null)
        {
            switcherPanel.SetActive(false);
        }
    }
}
