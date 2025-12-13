using UnityEngine;

/// <summary>
/// Container调试器 - 检查DeployedContainer消失的原因
/// </summary>
public class ContainerDebugger : MonoBehaviour
{
    [Header("要检查的UI_SpiritPanel")]
    [SerializeField]
    private UI_SpiritPanel spiritPanel;

    private void Start()
    {
        Invoke(nameof(CheckContainers), 0.1f); // 延迟检查，确保所有Start()都已执行
        Invoke(nameof(CheckContainers), 1f); // 1秒后再检查一次
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            CheckContainers();
        }
    }

    [ContextMenu("检查Containers")]
    private void CheckContainers()
    {
        Debug.Log("========================================");
        Debug.Log("===== Container状态检查 =====");
        Debug.Log("========================================");

        if (spiritPanel == null)
        {
            spiritPanel = FindObjectOfType<UI_SpiritPanel>();
        }

        if (spiritPanel == null)
        {
            Debug.LogError("❌ 找不到 UI_SpiritPanel!");
            return;
        }

        Debug.Log($"✅ 找到 UI_SpiritPanel: {spiritPanel.gameObject.name}");
        Debug.Log($"   GameObject.activeSelf: {spiritPanel.gameObject.activeSelf}");
        Debug.Log($"   GameObject.activeInHierarchy: {spiritPanel.gameObject.activeInHierarchy}");

        // 使用反射获取私有字段
        var panelType = spiritPanel.GetType();

        var deployedContainerField = panelType.GetField("deployedSlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ownedContainerField = panelType.GetField("ownedSlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var panelRootField = panelType.GetField("panelRoot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var deployedContainer = deployedContainerField?.GetValue(spiritPanel) as Transform;
        var ownedContainer = ownedContainerField?.GetValue(spiritPanel) as Transform;
        var panelRoot = panelRootField?.GetValue(spiritPanel) as GameObject;

        Debug.Log("\n----- Panel Root 状态 -----");
        if (panelRoot == null)
        {
            Debug.LogError("❌ panelRoot 为 null!");
        }
        else
        {
            Debug.Log($"✅ panelRoot: {panelRoot.name}");
            Debug.Log($"   activeSelf: {panelRoot.activeSelf}");
            Debug.Log($"   activeInHierarchy: {panelRoot.activeInHierarchy}");
        }

        Debug.Log("\n----- Deployed Container 状态 -----");
        if (deployedContainer == null)
        {
            Debug.LogError("❌ deployedSlotsContainer 为 null!");
            Debug.LogError("   原因可能是：");
            Debug.LogError("   1. Inspector中没有赋值");
            Debug.LogError("   2. 引用的GameObject被删除了");
            Debug.LogError("   3. 赋值错误（拖入了错误的对象）");
        }
        else
        {
            Debug.Log($"✅ deployedSlotsContainer 引用存在: {deployedContainer.name}");

            // 检查GameObject是否存在
            if (deployedContainer.gameObject == null)
            {
                Debug.LogError("❌ deployedContainer.gameObject 为 null!");
            }
            else
            {
                Debug.Log($"   GameObject 名称: {deployedContainer.gameObject.name}");
                Debug.Log($"   activeSelf: {deployedContainer.gameObject.activeSelf}");
                Debug.Log($"   activeInHierarchy: {deployedContainer.gameObject.activeInHierarchy}");

                // 检查位置
                Debug.Log($"   localPosition: {deployedContainer.localPosition}");
                Debug.Log($"   localScale: {deployedContainer.localScale}");

                // 检查RectTransform
                var rectTransform = deployedContainer.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Debug.Log($"   RectTransform.sizeDelta: {rectTransform.sizeDelta}");
                    Debug.Log($"   RectTransform.anchoredPosition: {rectTransform.anchoredPosition}");
                }

                // 检查子对象
                Debug.Log($"   子对象数量: {deployedContainer.childCount}");
                for (int i = 0; i < deployedContainer.childCount; i++)
                {
                    var child = deployedContainer.GetChild(i);
                    Debug.Log($"      [{i}] {child.name} (active: {child.gameObject.activeSelf})");
                }

                // 检查父对象
                if (deployedContainer.parent != null)
                {
                    Debug.Log($"   父对象: {deployedContainer.parent.name}");
                    Debug.Log($"   父对象 activeSelf: {deployedContainer.parent.gameObject.activeSelf}");
                }
                else
                {
                    Debug.LogWarning("⚠️ deployedContainer 没有父对象!");
                }
            }
        }

        Debug.Log("\n----- Owned Container 状态 -----");
        if (ownedContainer == null)
        {
            Debug.LogError("❌ ownedSlotsContainer 为 null!");
        }
        else
        {
            Debug.Log($"✅ ownedSlotsContainer 引用存在: {ownedContainer.name}");
            Debug.Log($"   GameObject 名称: {ownedContainer.gameObject.name}");
            Debug.Log($"   activeSelf: {ownedContainer.gameObject.activeSelf}");
            Debug.Log($"   activeInHierarchy: {ownedContainer.gameObject.activeInHierarchy}");
            Debug.Log($"   子对象数量: {ownedContainer.childCount}");
        }

        Debug.Log("\n========================================");
        Debug.Log("检查完成！按 F11 可以再次检查");
        Debug.Log("========================================");
    }
}
