using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单的 Image 填充条，用作血条/蓝条。
/// 期望在场景中将一个填充类型为 Filled 的 Image 赋给 `fillImage`。
/// </summary>
public class ImageBar : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;

    public void Set(float current, float max)
    {
        if (fillImage == null)
            return;

        if (max <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
