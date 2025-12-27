using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个Spirit槽位组件
/// </summary>
public class ItemUseSlot : MonoBehaviour
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

    [Header("Auto Populate")] 
    [SerializeField]
    private bool autoLoadFromPlayerData = false;

    [SerializeField]
    private PlayerData playerDataOverride;

    [Header("Key Activation")]
    [SerializeField]
    private bool enableKeyActivation = true;

    [SerializeField]
    private KeyCode activateKey = KeyCode.I;

    private Button button;
    [SerializeField]
    private int slotIndex = -1;
    private SpiritData spiritData;
    private System.Action<int> onClickCallback;
    private int currentHP;
    private int maxHP;
    private int currentMP;
    private int maxMP;
    private bool isAlive;
    private Player subscribedPlayer;
    private PlayerManager subscribedManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        // 强制从自己的子物体查找组件（忽略 Inspector 中的设置）
        spiritIcon = transform.Find("Icon")?.GetComponent<Image>();
        if (spiritIcon == null)
            spiritIcon = transform.Find("SpiritIcon")?.GetComponent<Image>();
        if (spiritIcon == null)
            spiritIcon = transform.Find("Image")?.GetComponent<Image>();

        background = GetComponent<Image>();

        nameText = transform.Find("NameText")?.GetComponent<TMP_Text>();
        if (nameText == null)
            nameText = transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>();

        hpText = transform.Find("HPText")?.GetComponent<TMP_Text>();
        if (hpText == null)
            hpText = transform.Find("HP")?.GetComponent<TMP_Text>();

        mpText = transform.Find("MPText")?.GetComponent<TMP_Text>();
        if (mpText == null)
            mpText = transform.Find("MP")?.GetComponent<TMP_Text>();

        Debug.Log($"[ItemUseSlot] Awake: {gameObject.name}, nameText={(nameText != null ? nameText.gameObject.name : "NULL")}, spiritIcon={(spiritIcon != null ? spiritIcon.gameObject.name : "NULL")}");
    }

    private void OnEnable()
    {
        if (autoLoadFromPlayerData)
        {
            SubscribeToPlayerEvents();
            SubscribeToManagerEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerEvents();
        UnsubscribeFromManagerEvents();
    }

    private void Start()
    {
        // 如果已经通过 Initialize 设置了 spiritData，不要覆盖
        if (spiritData != null)
        {
            Debug.Log($"[ItemUseSlot {slotIndex}] Start: spiritData 已设置为 {spiritData.DisplayName}，跳过自动加载");
            return;
        }

        if (autoLoadFromPlayerData)
        {
            if (slotIndex < 0)
            {
                slotIndex = transform.GetSiblingIndex();
            }

            AutoPopulateFromPlayerData();
        }
    }

    private void Update()
    {
        // 只有在已初始化（slotIndex >= 0）且启用按键激活时才响应
        if (!enableKeyActivation || slotIndex < 0)
            return;

        if (Input.GetKeyDown(activateKey))
        {
            ActivateSlot();
        }
    }

    /// <summary>
    /// 获取当前槽位的精灵数据
    /// </summary>
    public SpiritData GetSpiritData()
    {
        return spiritData;
    }

    public void SetAutoLoadFromPlayerData(bool enabled)
    {
        if (autoLoadFromPlayerData == enabled)
            return;

        if (!enabled)
        {
            UnsubscribeFromPlayerEvents();
            UnsubscribeFromManagerEvents();
        }

        autoLoadFromPlayerData = enabled;
    }

    public bool IsAutoLoadingFromPlayerData()
    {
        return autoLoadFromPlayerData;
    }

    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Initialize(int index, SpiritData data, System.Action<int> onClick)
    {
        Debug.Log($"[ItemUseSlot {index}] Initialize 被调用, data={(data != null ? data.DisplayName : "NULL")}, autoLoadFromPlayerData={autoLoadFromPlayerData}");
        
        slotIndex = index;
        onClickCallback = onClick;

        // 自动模式下，忽略传入的data，直接从玩家拥有列表读取
        if (autoLoadFromPlayerData)
        {
            Debug.Log($"[ItemUseSlot {index}] 走自动模式分支");
            spiritData = null;
            isAlive = false;
            currentHP = currentMP = maxHP = maxMP = 0;

            EnsureButtonSetup();
            EnsureComponentsActive();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }

            AutoPopulateFromPlayerData();
            return;
        }

        Debug.Log($"[ItemUseSlot {index}] 走手动模式分支，设置 spiritData={data?.DisplayName}");
        spiritData = data;

        if (spiritData != null)
        {
            maxHP = currentHP = spiritData.MaxHP;
            maxMP = currentMP = spiritData.MaxMana;
            isAlive = true;
        }
        else
        {
            isAlive = false;
            currentHP = currentMP = maxHP = maxMP = 0;
        }

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

    private void ActivateSlot()
    {
        ActivateHierarchyIfNeeded();
        gameObject.SetActive(true);

        // 自动模式下按键激活时也刷新拥有精灵
        if (autoLoadFromPlayerData)
        {
            AutoPopulateFromPlayerData();
        }
        else
        {
            EnsureButtonSetup();
            EnsureComponentsActive();
            UpdateDisplay();
        }
    }

    private void ActivateHierarchyIfNeeded()
    {
        // 先保证所有祖先节点激活，再激活自身
        var stack = new System.Collections.Generic.Stack<Transform>();
        var current = transform.parent;
        while (current != null)
        {
            stack.Push(current);
            current = current.parent;
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
            }
        }
    }

    private void AutoPopulateFromPlayerData()
    {
        var ownedSpirits = GetOwnedSpiritsFromPlayerData();

        if (ownedSpirits == null || ownedSpirits.Count == 0)
        {
            Debug.LogWarning($"SpiritSlot {slotIndex}: 未找到拥有的精灵数据");
            ClearSlotDisplay();
            return;
        }

        int targetIndex = slotIndex >= 0 ? slotIndex : transform.GetSiblingIndex();
        if (targetIndex < 0 || targetIndex >= ownedSpirits.Count)
        {
            Debug.LogWarning($"SpiritSlot {slotIndex}: 索引超出范围，拥有精灵数={ownedSpirits.Count}");
            ClearSlotDisplay();
            return;
        }

        spiritData = ownedSpirits[targetIndex];
        if (spiritData != null)
        {
            maxHP = currentHP = spiritData.MaxHP;
            maxMP = currentMP = spiritData.MaxMana;
            isAlive = true;
        }
        else
        {
            isAlive = false;
            currentHP = currentMP = maxHP = maxMP = 0;
        }

        EnsureButtonSetup();
        EnsureComponentsActive();
        UpdateDisplay();
    }

    private void SubscribeToPlayerEvents()
    {
        UnsubscribeFromPlayerEvents();

        if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayer != null)
        {
            subscribedPlayer = PlayerManager.Instance.CurrentPlayer;
            subscribedPlayer.OnDataChanged += OnPlayerDataChanged;
            Debug.Log($"SpiritSlot {slotIndex}: 订阅 Player.OnDataChanged");
        }
    }

    private void SubscribeToManagerEvents()
    {
        UnsubscribeFromManagerEvents();

        if (PlayerManager.Instance != null)
        {
            subscribedManager = PlayerManager.Instance;
            subscribedManager.OwnedSpiritsChanged += OnOwnedSpiritsChanged;
            Debug.Log($"SpiritSlot {slotIndex}: 订阅 PlayerManager.OwnedSpiritsChanged");
        }
    }

    private void UnsubscribeFromManagerEvents()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OwnedSpiritsChanged -= OnOwnedSpiritsChanged;
            Debug.Log($"SpiritSlot {slotIndex}: 取消订阅 PlayerManager.OwnedSpiritsChanged");
            subscribedManager = null;
        }
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDataChanged -= OnPlayerDataChanged;
            Debug.Log($"SpiritSlot {slotIndex}: 取消订阅 Player.OnDataChanged");
            subscribedPlayer = null;
        }
    }

    private void OnPlayerDataChanged()
    {
        if (!autoLoadFromPlayerData)
            return;

        AutoPopulateFromPlayerData();
    }

    private void OnOwnedSpiritsChanged()
    {
        if (!autoLoadFromPlayerData)
            return;

        AutoPopulateFromPlayerData();
    }

    private List<SpiritData> GetOwnedSpiritsFromPlayerData()
    {
        if (PlayerManager.Instance != null)
        {
            var owned = PlayerManager.Instance.GetOwnedSpirits();
            if (owned != null && owned.Count > 0)
            {
                return owned;
            }
        }

        var data = ResolvePlayerData();
        return data != null ? data.GetOwnedSpirits() : null;
    }

    private PlayerData ResolvePlayerData()
    {
        if (playerDataOverride != null)
        {
            return playerDataOverride;
        }

        var allData = Resources.FindObjectsOfTypeAll<PlayerData>();
        if (allData != null && allData.Length > 0)
        {
            return allData[0];
        }

        return null;
    }

    private void ClearSlotDisplay()
    {
        spiritData = null;
        onClickCallback = null;
        isAlive = false;
        currentHP = currentMP = maxHP = maxMP = 0;

        EnsureButtonSetup();
        EnsureComponentsActive();
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
        Debug.Log($"[ItemUseSlot {slotIndex}] UpdateDisplay 开始, spiritData={(spiritData != null ? spiritData.DisplayName : "NULL")}");
        Debug.Log($"[ItemUseSlot {slotIndex}] 调用栈:\n{System.Environment.StackTrace}");
        Debug.Log($"[ItemUseSlot {slotIndex}] spiritIcon={(spiritIcon != null ? "有效" : "NULL")}");
        Debug.Log($"[ItemUseSlot {slotIndex}] nameText={(nameText != null ? "有效" : "NULL")}");
        Debug.Log($"[ItemUseSlot {slotIndex}] hpText={(hpText != null ? "有效" : "NULL")}");
        Debug.Log($"[ItemUseSlot {slotIndex}] mpText={(mpText != null ? "有效" : "NULL")}");
        Debug.Log($"[ItemUseSlot {slotIndex}] GameObject active={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
        
        if (spiritData != null)
        {
            // 有Spirit数据
            if (spiritIcon != null)
            {
                spiritIcon.enabled = true;
                spiritIcon.sprite = spiritData.Image;
                Debug.Log($"[ItemUseSlot {slotIndex}] 设置图标: sprite={(spiritData.Image != null ? spiritData.Image.name : "NULL")}");

                // 死亡的Spirit图标变灰
                spiritIcon.color = isAlive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = spiritData.DisplayName;
                Debug.Log($"[ItemUseSlot {slotIndex}] 设置名称: {spiritData.DisplayName}, nameText所在物体={nameText.transform.parent?.name}/{nameText.gameObject.name}, 自身物体={gameObject.name}");
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
            Debug.LogWarning($"[ItemUseSlot {slotIndex}] spiritData 为空，显示 Empty!!! 调用栈:\n{System.Environment.StackTrace}");
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
                Debug.LogWarning($"[ItemUseSlot {slotIndex}] 设置Empty! nameText所在物体={nameText.transform.parent?.name}/{nameText.gameObject.name}, 自身物体={gameObject.name}");
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
