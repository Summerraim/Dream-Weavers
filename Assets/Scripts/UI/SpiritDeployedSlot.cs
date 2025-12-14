using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 已出场Spirit槽位组件（用于Spirit管理面板）
/// </summary>
public class SpiritDeployedSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Image spiritIcon;

    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text statsText; // 显示HP/MP/ATK等

    [SerializeField]
    private GameObject selectedIndicator; // 选中指示器

    private Button button;
    private int slotIndex;
    private SpiritData spiritData;
    private System.Action<int> onClickCallback;

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
                background.color = new Color(0.3f, 0.5f, 0.3f, 0.8f); // 绿色表示已出场
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
                spiritIcon.color = Color.white;
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = spiritData.DisplayName;
            }

            if (statsText != null)
            {
                statsText.enabled = true;
                statsText.text = $"HP:{spiritData.MaxHP} MP:{spiritData.MaxMana}\nATK:{spiritData.Damage} DEF:{spiritData.Defense}";
            }

            if (background != null)
            {
                background.enabled = true;
                background.color = new Color(0.3f, 0.5f, 0.3f, 0.8f); // 绿色
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }
        else
        {
            // 空槽位
            if (spiritIcon != null)
            {
                spiritIcon.sprite = null;
                spiritIcon.enabled = false;
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = "Empty";
                nameText.color = new Color(0.5f, 0.5f, 0.5f);
            }

            if (statsText != null)
            {
                statsText.enabled = false;
                statsText.text = "";
            }

            if (background != null)
            {
                background.enabled = true;
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 灰色
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
                : new Color(0.3f, 0.5f, 0.3f, 0.8f); // 绿色
        }
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
    /// 按钮点击回调
    /// </summary>
    private void OnClick()
    {
        Debug.Log($"SpiritDeployedSlot {slotIndex}: OnClick, spirit={spiritData?.DisplayName ?? "null"}");
        onClickCallback?.Invoke(slotIndex);
    }
}
