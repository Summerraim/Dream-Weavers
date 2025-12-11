using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个Effect（Buff/Debuff）槽位组件
/// 用于显示战斗中角色身上的持续效果
/// </summary>
public class EffectSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Image effectIcon;

    [SerializeField]
    private Image background;

    [SerializeField]
    private TMP_Text durationText;

    [Header("Effect Type Colors")]
    [SerializeField]
    private Color buffColor = new Color(0.2f, 0.8f, 0.3f, 0.8f); // 绿色 - Buff

    [SerializeField]
    private Color debuffColor = new Color(0.9f, 0.2f, 0.2f, 0.8f); // 红色 - Debuff

    [SerializeField]
    private Color controlDebuffColor = new Color(0.8f, 0.4f, 0.9f, 0.8f); // 紫色 - 控制Debuff

    [SerializeField]
    private Color permanentColor = new Color(0.3f, 0.6f, 0.9f, 0.8f); // 蓝色 - 永久效果

    private Buff buffData;

    private void Awake()
    {
        // 自动查找子组件（如果未在Inspector中设置）
        if (effectIcon == null)
            effectIcon = transform.Find("Icon")?.GetComponent<Image>();

        if (background == null)
            background = GetComponent<Image>();

        if (durationText == null)
            durationText = transform.Find("DurationText")?.GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 设置Effect数据并更新显示
    /// </summary>
    public void SetEffect(Buff buff)
    {
        buffData = buff;

        // 添加调试日志
        if (buff != null)
        {
            Debug.Log($"[EffectSlot] SetEffect: {buff.DisplayName}, " +
                     $"RemainingTurns={buff.RemainingTurns}, " +
                     $"SourceEffect={(buff.SourceEffect != null ? buff.SourceEffect.name : "null")}, " +
                     $"Image={(buff.Image != null ? "存在" : "null")}");
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        buffData = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新槽位显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (buffData == null)
        {
            Clear();
            return;
        }

        gameObject.SetActive(true);

        Debug.Log($"[EffectSlot] UpdateDisplay开始: {buffData.DisplayName}");

        // 设置持续回合数
        if (durationText != null)
        {
            if (buffData.RemainingTurns < 0)
            {
                durationText.text = "∞"; // 永久效果
            }
            else
            {
                durationText.text = buffData.RemainingTurns.ToString();
            }
            durationText.enabled = true;
            durationText.gameObject.SetActive(true);

            Debug.Log($"[EffectSlot] Duration设置完成: {durationText.text}");
        }
        else
        {
            Debug.LogWarning($"[EffectSlot] durationText is null!");
        }

        // 根据Effect类型设置背景颜色
        if (background != null)
        {
            background.color = GetEffectColor(buffData);
            Debug.Log($"[EffectSlot] 背景颜色设置完成");
        }
        else
        {
            Debug.LogWarning($"[EffectSlot] background is null!");
        }

        // 设置图标（直接从Buff获取Image）
        if (effectIcon != null)
        {
            if (buffData.Image != null)
            {
                effectIcon.sprite = buffData.Image;
                effectIcon.enabled = true;
                effectIcon.color = Color.white;
                effectIcon.gameObject.SetActive(true);

                Debug.Log($"[EffectSlot] 图标设置完成: {buffData.Image.name}");
            }
            else
            {
                // 没有图标时隐藏
                effectIcon.enabled = false;
                Debug.LogWarning($"[EffectSlot] buffData.Image is null! SourceEffect={(buffData.SourceEffect != null ? buffData.SourceEffect.name : "null")}");
            }
        }
        else
        {
            Debug.LogWarning($"[EffectSlot] effectIcon is null!");
        }

        Debug.Log($"[EffectSlot] UpdateDisplay完成");
    }

    /// <summary>
    /// 根据Buff类型获取对应的颜色
    /// </summary>
    private Color GetEffectColor(Buff buff)
    {
        if (buff == null)
            return Color.gray;

        // 永久效果
        if (buff.RemainingTurns < 0)
            return permanentColor;

        // 检查是否为Debuff（通过类型名或类型检查）
        string typeName = buff.GetType().Name;

        // 控制型Debuff（最优先）
        if (
            buff is FrozenDebuff
            || buff is SleepDebuff
            || buff is ConfusionDebuff
            || typeName.Contains("Control")
        )
        {
            return controlDebuffColor;
        }

        // 普通Debuff
        if (
            IsDebuff(buff)
            || typeName.Contains("Debuff")
            || buff is PoisonDebuff
            || buff is BurnDebuff
            || buff is BlindDebuff
            || buff is SilenceDebuff
            || buff is WeakenAttackDebuff
            || buff is WeakenDefenseDebuff
            || buff is WeakenBuff
            || buff is VulnerabilityDebuff
            || buff is HealingReductionDebuff
            || buff is ManaLeechDebuff
            || buff is CurseDebuff
        )
        {
            return debuffColor;
        }

        // 增益Buff
        return buffColor;
    }

    /// <summary>
    /// 判断是否为Debuff
    /// </summary>
    private bool IsDebuff(Buff buff)
    {
        if (buff == null)
            return false;

        // 检查类名是否包含"Debuff"
        string typeName = buff.GetType().Name;
        return typeName.Contains("Debuff");
    }

    /// <summary>
    /// 刷新显示（用于每回合更新）
    /// </summary>
    public void Refresh()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// 获取当前显示的Buff数据
    /// </summary>
    public Buff GetBuff()
    {
        return buffData;
    }
}
