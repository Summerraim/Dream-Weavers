using UnityEngine;

/// <summary>
/// Spirit管理面板输入控制器
/// 负责处理打开Spirit管理面板的快捷键
/// </summary>
public class SpiritPanelInput : MonoBehaviour
{
    [Header("面板控制器引用")]
    [SerializeField]
    private SpiritPanelController spiritPanelController;

    [Header("快捷键设置")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Space; // 默认按空格键

    private void Update()
    {
        // 按下快捷键时切换面板显示/隐藏
        if (Input.GetKeyDown(toggleKey))
        {
            if (spiritPanelController != null)
            {
                spiritPanelController.TogglePanel();
            }
            else
            {
                Debug.LogWarning("[SpiritPanelInput] SpiritPanelController is not assigned!");
            }
        }
    }
}
