using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个Spirit槽位组件
/// </summary>
public class SpiritSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Image spiritIcon;

    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text hpText;

    [SerializeField]
    private TMP_Text mpText;

    private Button button;
    private int slotIndex;
    private SpiritData spiritData;
    private System.Action<int> onClickCallback;
    private int currentHP;
    private int maxHP;
    private int currentMP;
    private int maxMP;
    private bool isAlive;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        // 自动查找子组件（如果未在Inspector中设置）
        if (spiritIcon == null)
            spiritIcon = transform.Find("Icon")?.GetComponent<Image>();

        if (background == null)
            background = GetComponent<Image>();

        if (nameText == null)
            nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();

        if (hpText == null)
            hpText = transform.Find("HPText")?.GetComponent<TMP_Text>();

        if (mpText == null)
            mpText = transform.Find("MPText")?.GetComponent<TMP_Text>();
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

        // 确保所有组件激活
        EnsureComponentsActive();

        // 设置按钮点击事件
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            Debug.Log($"SpiritSlot {index}: Button click listener added");
        }
        else
        {
            Debug.LogError($"SpiritSlot {index}: Button is null after setup!");
        }

        // 更新显示
        UpdateDisplay();
    }

    /// <summary>
    /// 确保Button组件正确设置
    /// </summary>
    private void EnsureButtonSetup()
    {
        // 确保有Button组件
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
        }

        // 确保有Image组件作为RaycastTarget
        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
        }

        // 确保Image的raycastTarget开启
        if (background != null)
        {
            background.raycastTarget = true;
        }

        // 确保Button引用了正确的Image
        button.targetGraphic = background;
    }

    /// <summary>
    /// 确保所有UI组件都激活
    /// </summary>
    private void EnsureComponentsActive()
    {
        if (spiritIcon != null)
        {
            spiritIcon.enabled = true;
            spiritIcon.gameObject.SetActive(true);
        }

        if (background != null)
        {
            background.enabled = true;
            background.gameObject.SetActive(true);
        }

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.gameObject.SetActive(true);
        }

        if (hpText != null)
        {
            hpText.enabled = true;
            hpText.gameObject.SetActive(true);
        }

        if (mpText != null)
        {
            mpText.enabled = true;
            mpText.gameObject.SetActive(true);
        }

        if (button != null)
        {
            button.enabled = true;
        }
    }

    /// <summary>
    /// 更新槽位显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (spiritData != null)
        {
            // 有Spirit数据
            if (spiritIcon != null)
            {
                spiritIcon.enabled = true;
                spiritIcon.sprite = spiritData.Image;

                // 死亡的Spirit图标变灰
                spiritIcon.color = isAlive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = spiritData.DisplayName;
            }

            // 显示HP/MP
            if (hpText != null)
            {
                hpText.enabled = true;
                hpText.text = $"HP: {currentHP}/{maxHP}";
            }

            if (mpText != null)
            {
                mpText.enabled = true;
                mpText.text = $"MP: {currentMP}/{maxMP}";
            }

            if (background != null)
            {
                background.enabled = true;
            }

            if (button != null)
            {
                // 死亡的Spirit不可点击
                button.interactable = isAlive;
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
            }

            if (hpText != null)
            {
                hpText.enabled = false;
                hpText.text = "";
            }

            if (mpText != null)
            {
                mpText.enabled = false;
                mpText.text = "";
            }

            if (background != null)
            {
                background.enabled = true;
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// 设置选中状态（通过背景颜色）
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (background != null)
        {
            // 改变背景颜色
            background.color = isSelected
                ? new Color(1f, 0.8f, 0.3f, 0.8f) // 黄色高亮
                : new Color(0.2f, 0.2f, 0.2f, 0.8f); // 灰色
        }
    }

    /// <summary>
    /// 更新Spirit状态（HP/MP和存活状态）
    /// </summary>
    public void UpdateStatus(int hp, int maxHp, int mp, int maxMp, bool alive)
    {
        currentHP = hp;
        maxHP = maxHp;
        currentMP = mp;
        maxMP = maxMp;
        isAlive = alive;

        UpdateDisplay();
    }

    /// <summary>
    /// 按钮点击回调
    /// </summary>
    private void OnClick()
    {
        Debug.Log(
            $"SpiritSlot {slotIndex}: OnClick called, spiritData={(spiritData != null ? spiritData.DisplayName : "null")}"
        );

        if (spiritData != null)
        {
            Debug.Log($"SpiritSlot {slotIndex}: Invoking callback");
            onClickCallback?.Invoke(slotIndex);
        }
        else
        {
            Debug.LogWarning($"SpiritSlot {slotIndex}: Cannot click, spiritData is null");
        }
    }
}
