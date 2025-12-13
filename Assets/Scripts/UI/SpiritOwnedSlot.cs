using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拥有的Spirit槽位组件（用于Spirit管理面板）
/// </summary>
public class SpiritOwnedSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Image spiritIcon;

    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text statsText;

    [SerializeField]
    private GameObject deployedIndicator; // 已出场指示器

    [SerializeField]
    private GameObject selectedIndicator; // 选中指示器

    private Button button;
    private int slotIndex;
    private SpiritData spiritData;
    private System.Action<int> onClickCallback;
    private bool isDeployed; // 是否已出场

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        // 自动查找子组件
        if (spiritIcon == null)
            spiritIcon = transform.Find("Icon")?.GetComponent<Image>();

        if (background == null)
            background = GetComponent<Image>();

        if (nameText == null)
            nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();

        if (statsText == null)
            statsText = transform.Find("StatsText")?.GetComponent<TMP_Text>();

        if (deployedIndicator == null)
            deployedIndicator = transform.Find("DeployedIndicator")?.gameObject;

        if (selectedIndicator == null)
            selectedIndicator = transform.Find("SelectedIndicator")?.gameObject;
    }

    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Initialize(int index, SpiritData data, System.Action<int> onClick)
    {
        slotIndex = index;
        spiritData = data;
        onClickCallback = onClick;

        // 确保Button组件正确配置
        EnsureButtonSetup();

        // 设置按钮点击事件
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        // 初始时不显示选中指示器
        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);

        // 更新显示
        UpdateDisplay();
    }

    /// <summary>
    /// 确保Button组件正确设置
    /// </summary>
    private void EnsureButtonSetup()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
        }

        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 灰色
            }
        }

        background.raycastTarget = true;
        button.targetGraphic = background;
    }

    /// <summary>
    /// 更新槽位显示
    /// </summary>
    public void UpdateDisplay()
    {
        if (spiritData != null)
        {
            // 有Spirit数据
            if (spiritIcon != null)
            {
                spiritIcon.enabled = true;
                spiritIcon.sprite = spiritData.Image;
                spiritIcon.color = isDeployed ? new Color(0.7f, 0.7f, 0.7f) : Color.white; // 已出场则变暗
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = spiritData.DisplayName;
                nameText.color = isDeployed ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            }

            if (statsText != null)
            {
                statsText.enabled = true;
                statsText.text = $"HP:{spiritData.MaxHP} MP:{spiritData.MaxMana}\nATK:{spiritData.Damage} DEF:{spiritData.Defense}";
                statsText.color = isDeployed ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            }

            if (background != null)
            {
                background.enabled = true;
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            // 显示已出场指示器
            if (deployedIndicator != null)
            {
                deployedIndicator.SetActive(isDeployed);
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }
        else
        {
            // 空槽位（不应该出现）
            if (spiritIcon != null)
            {
                spiritIcon.sprite = null;
                spiritIcon.enabled = false;
            }

            if (nameText != null)
            {
                nameText.enabled = false;
            }

            if (statsText != null)
            {
                statsText.enabled = false;
            }

            if (deployedIndicator != null)
            {
                deployedIndicator.SetActive(false);
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected);
        }

        if (background != null && spiritData != null)
        {
            background.color = isSelected
                ? new Color(1f, 0.8f, 0.3f, 0.8f) // 黄色高亮
                : new Color(0.2f, 0.2f, 0.2f, 0.8f); // 灰色
        }
    }

    /// <summary>
    /// 设置已出场状态
    /// </summary>
    public void SetDeployed(bool deployed)
    {
        isDeployed = deployed;
        UpdateDisplay();
    }

    /// <summary>
    /// 获取当前Spirit数据
    /// </summary>
    public SpiritData GetSpiritData()
    {
        return spiritData;
    }

    /// <summary>
    /// 获取槽位索引
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }

    /// <summary>
    /// 是否已出场
    /// </summary>
    public bool IsDeployed()
    {
        return isDeployed;
    }

    /// <summary>
    /// 按钮点击回调
    /// </summary>
    private void OnClick()
    {
        Debug.Log($"SpiritOwnedSlot {slotIndex}: OnClick, spirit={spiritData?.DisplayName ?? "null"}, deployed={isDeployed}");
        onClickCallback?.Invoke(slotIndex);
    }
}
