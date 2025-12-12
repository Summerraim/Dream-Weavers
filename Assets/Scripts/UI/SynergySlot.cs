using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个Synergy（羁绊）槽位组件
/// 用于显示Spirit身上的羁绊效果
/// </summary>
public class SynergySlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Image synergyIcon;

    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text tierText;

    [Header("Tier Colors")]
    [SerializeField]
    private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 灰色 - 未激活

    [SerializeField]
    private Color tier1Color = new Color(0.6f, 0.6f, 0.9f, 0.8f); // 浅蓝色 - 1档

    [SerializeField]
    private Color tier2Color = new Color(0.3f, 0.6f, 0.9f, 0.8f); // 蓝色 - 2档

    [SerializeField]
    private Color tier3Color = new Color(0.9f, 0.6f, 0.2f, 0.8f); // 金色 - 3档

    private SynergyModel synergyData;

    private void Awake()
    {
        // 自动查找子组件（如果未在Inspector中设置）
        if (synergyIcon == null)
            synergyIcon = transform.Find("Icon")?.GetComponent<Image>();

        if (background == null)
            background = GetComponent<Image>();

        if (tierText == null)
            tierText = transform.Find("TierText")?.GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 设置Synergy数据并更新显示
    /// </summary>
    public void SetSynergy(SynergyModel synergy)
    {
        synergyData = synergy;

        if (synergy != null)
        {
            Debug.Log(
                $"[SynergySlot] SetSynergy: {synergy.Synergy.DisplayName}, "
                    + $"ActiveCount={synergy.ActiveCount}, "
                    + $"TierIndex={synergy.GetCurrentTierIndex()}"
            );
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        synergyData = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新槽位显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (synergyData == null || synergyData.Synergy == null)
        {
            Clear();
            return;
        }

        gameObject.SetActive(true);

        Debug.Log($"[SynergySlot] UpdateDisplay开始: {synergyData.Synergy.DisplayName}");

        // 获取当前档位
        int tierIndex = synergyData.GetCurrentTierIndex();
        int activeCount = synergyData.ActiveCount;

        // 设置档位文本（显示当前活跃数量）
        if (tierText != null)
        {
            tierText.text = activeCount.ToString();
            tierText.enabled = true;
            tierText.gameObject.SetActive(true);
            Debug.Log($"[SynergySlot] ActiveCount设置完成: {activeCount}");
        }

        // 根据档位设置背景颜色
        if (background != null)
        {
            background.color = GetTierColor(tierIndex);
            Debug.Log($"[SynergySlot] 背景颜色设置完成，档位={tierIndex}");
        }

        // 设置图标
        if (synergyIcon != null)
        {
            if (synergyData.Synergy.Image != null)
            {
                synergyIcon.sprite = synergyData.Synergy.Image;
                synergyIcon.enabled = true;
                synergyIcon.color = Color.white;
                synergyIcon.gameObject.SetActive(true);

                Debug.Log($"[SynergySlot] 图标设置完成: {synergyData.Synergy.Image.name}");
            }
            else
            {
                // 没有图标时隐藏
                synergyIcon.enabled = false;
                Debug.LogWarning(
                    $"[SynergySlot] synergyData.Synergy.Image is null! Synergy={synergyData.Synergy.name}"
                );
            }
        }

        Debug.Log($"[SynergySlot] UpdateDisplay完成");
    }

    /// <summary>
    /// 根据档位获取对应的颜色
    /// </summary>
    private Color GetTierColor(int tierIndex)
    {
        switch (tierIndex)
        {
            case -1:
                return inactiveColor; // 未激活
            case 0:
                return tier1Color; // 1档
            case 1:
                return tier2Color; // 2档
            case 2:
            default:
                return tier3Color; // 3档及以上
        }
    }

    /// <summary>
    /// 刷新显示（用于每回合更新）
    /// </summary>
    public void Refresh()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// 获取当前显示的Synergy数据
    /// </summary>
    public SynergyModel GetSynergy()
    {
        return synergyData;
    }
}
