using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
=======
using TMPro;
using UnityEngine.UI;

>>>>>>> Stashed changes

/// <summary>
/// 背包槽位UI
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI引用")]
    public Image iconImage;           // 物品图标
    public TextMeshProUGUI quantityText;         // 数量文本
    public GameObject selectedIndicator; // 选中指示器
    public Sprite fallbackIcon; // 当物品没有图标时的占位图标（可选）
    
    [Header("槽位设置")]
    public int slotIndex = -1; // 槽位索引
    public bool isEquipmentSlot = false; // 是否是装备槽

    private InventoryItem currentItem; // 当前物品

    // 事件
    public System.Action<InventorySlot> OnSlotClicked;
    public System.Action<InventorySlot> OnSlotBeginDrag;
    public System.Action<InventorySlot> OnSlotEndDrag;
    public System.Action<InventorySlot, InventorySlot> OnSlotDrop;
    public System.Action<InventorySlot> OnSlotHoverEnter;
    public System.Action<InventorySlot> OnSlotHoverExit;
    
    /// <summary>
    /// 更新槽位显示
    /// </summary>
    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item;

        if (item != null && item.data != null && item.quantity > 0)
        {
            // 显示物品
            var sprite = item.data.Icon != null ? item.data.Icon : fallbackIcon;
            iconImage.sprite = sprite;
            iconImage.color = sprite != null ? Color.white : new Color(0, 0, 0, 0);
            quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
            item.slotIndex = slotIndex;
        }
        else
        {
            // 清空槽位
            ClearSlot();
        }
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
        iconImage.color = new Color(0, 0, 0, 0);
        quantityText.text = "";

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }

    /// <summary>
    /// 获取当前物品
    /// </summary>
    public InventoryItem GetItem()
    {
        return currentItem;
    }

    #region UI事件处理

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 左键点击：使用物品
            if (currentItem != null)
            {
                InventoryManager.Instance.UseItem(currentItem);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 右键点击：显示详细信息
            OnSlotClicked?.Invoke(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        OnSlotBeginDrag?.Invoke(this);

        // 开始拖拽时隐藏原始图标
        iconImage.color = new Color(1, 1, 1, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽逻辑在InventoryUIController中处理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnSlotEndDrag?.Invoke(this);

        // 恢复图标显示
        if (currentItem != null)
            iconImage.color = Color.white;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 处理物品放入槽位
        GameObject dragObject = eventData.pointerDrag;
        if (dragObject != null)
        {
            InventorySlot sourceSlot = dragObject.GetComponent<InventorySlot>();
            if (sourceSlot != null && sourceSlot != this)
            {
                OnSlotDrop?.Invoke(sourceSlot, this);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 鼠标进入：通知悬停开始
        OnSlotHoverEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标离开：通知悬停结束
        OnSlotHoverExit?.Invoke(this);
    }

    

    #endregion
}
