using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仅用于外观展示的组件：给定 ItemData，渲染其 Icon。
/// 不包含碰撞体或交互逻辑，适用于纯视觉展示的道具房。
/// </summary>
public class ItemDisplay : MonoBehaviour
{
    [Header("数据")]
    [SerializeField]public ItemData item;

    // [Header("渲染绑定（二选一或都可）")]
    public SpriteRenderer spriteRenderer;
    // public Image uiImage;

    private void Awake()
    {
        Refresh();
    }

    /// <summary>
    /// 根据 ItemData.Icon 刷新显示。
    /// </summary>
    public void Refresh()
    {
        var icon = item != null ? item.Icon : null;
        if (spriteRenderer != null) spriteRenderer.sprite = icon;
        // if (uiImage != null) uiImage.sprite = icon;
    }
}
