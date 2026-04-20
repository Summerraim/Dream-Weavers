using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private TMP_Text statsText;

    [SerializeField]
    private GameObject selectedIndicator;

    [SerializeField]
    private SpiritIconSizeTable iconSizeTable;

    private Button button;
    private int slotIndex;
    private SpiritData spiritData;
    private System.Action<int> onClickCallback;
    private int currentHP;
    private int maxHP;
    private int currentMP;
    private int maxMP;
    private Vector2 defaultIconSize;
    private bool hasDefaultIconSize;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

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

        CacheDefaultIconSize();
    }

    public void Initialize(int index, SpiritData data, System.Action<int> onClick)
    {
        slotIndex = index;
        spiritData = data;
        onClickCallback = onClick;

        if (spiritData != null)
        {
            currentHP = spiritData.MaxHP;
            maxHP = spiritData.MaxHP;
            currentMP = spiritData.MaxMana;
            maxMP = spiritData.MaxMana;
        }

        EnsureButtonSetup();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);

        UpdateDisplay();
    }

    public void UpdateRuntimeData(int currentHp, int maxHp, int currentMp, int maxMp)
    {
        currentHP = currentHp;
        maxHP = maxHp;
        currentMP = currentMp;
        maxMP = maxMp;
        UpdateDisplay();
    }

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
                background.color = new Color(0.3f, 0.5f, 0.3f, 0.8f);
            }
        }

        background.raycastTarget = true;
        button.targetGraphic = background;
    }

    private void CacheDefaultIconSize()
    {
        if (spiritIcon == null || spiritIcon.rectTransform == null)
            return;

        defaultIconSize = spiritIcon.rectTransform.sizeDelta;
        hasDefaultIconSize = true;
    }

    private void ApplyIconSize(string displayName)
    {
        if (spiritIcon == null || spiritIcon.rectTransform == null)
            return;

        if (!hasDefaultIconSize)
            CacheDefaultIconSize();

        Vector2 targetSize = hasDefaultIconSize ? defaultIconSize : spiritIcon.rectTransform.sizeDelta;
        if (iconSizeTable != null && iconSizeTable.TryGetSize(displayName, out Vector2 mappedSize))
        {
            targetSize = mappedSize;
        }

        spiritIcon.rectTransform.sizeDelta = targetSize;
    }

    public void UpdateDisplay()
    {
        if (spiritData != null)
        {
            if (spiritIcon != null)
            {
                spiritIcon.enabled = true;
                spiritIcon.sprite = spiritData.Image;
                spiritIcon.color = Color.white;
                ApplyIconSize(spiritData.DisplayName);
            }

            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = spiritData.DisplayName;
            }

            if (statsText != null)
            {
                statsText.enabled = true;
                statsText.text = $"HP: {currentHP}/{maxHP}\nMP: {currentMP}/{maxMP}";
            }

            if (background != null)
            {
                background.enabled = true;
                background.color = new Color(0.3f, 0.5f, 0.3f, 0.8f);
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }
        else
        {
            if (spiritIcon != null)
            {
                spiritIcon.sprite = null;
                spiritIcon.enabled = false;
                ApplyIconSize(string.Empty);
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
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected);
        }

        if (background != null && spiritData != null)
        {
            background.color = isSelected
                ? new Color(1f, 0.8f, 0.3f, 0.8f)
                : new Color(0.3f, 0.5f, 0.3f, 0.8f);
        }
    }

    public SpiritData GetSpiritData()
    {
        return spiritData;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    private void OnClick()
    {
        Debug.Log($"SpiritDeployedSlot {slotIndex}: OnClick, spirit={spiritData?.DisplayName ?? "null"}");
        onClickCallback?.Invoke(slotIndex);
    }
}
