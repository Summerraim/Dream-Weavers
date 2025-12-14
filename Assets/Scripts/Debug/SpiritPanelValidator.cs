using UnityEngine;

/// <summary>
/// Spirit面板验证器 - 自动检查所有配置是否正确
/// 使用方法：将此脚本添加到场景中任何GameObject上，运行游戏查看Console
/// </summary>
public class SpiritPanelValidator : MonoBehaviour
{
    [Header("验证设置")]
    [SerializeField]
    private bool runOnStart = true; // 游戏开始时自动运行验证

    [SerializeField]
    private KeyCode manualValidateKey = KeyCode.F12; // 手动触发验证的按键

    private void Start()
    {
        if (runOnStart)
        {
            Invoke(nameof(ValidateSetup), 0.5f); // 延迟0.5秒确保所有Start()都已执行
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(manualValidateKey))
        {
            ValidateSetup();
        }
    }

    /// <summary>
    /// 执行完整的配置验证
    /// </summary>
    public void ValidateSetup()
    {
        Debug.Log("========================================");
        Debug.Log("===== Spirit面板配置验证开始 =====");
        Debug.Log("========================================");

        bool allValid = true;

        // 步骤1: 检查SpiritPanelController
        allValid &= ValidateSpiritPanelController();

        // 步骤2: 检查UI_SpiritPanel
        allValid &= ValidateUISpiritPanel();

        // 步骤3: 检查PlayerData
        allValid &= ValidatePlayerData();

        // 步骤4: 检查Player实例
        allValid &= ValidatePlayerInstance();

        // 步骤5: 检查输入系统
        ValidateInputSystem();

        Debug.Log("========================================");
        if (allValid)
        {
            Debug.Log("✅ <color=green><b>所有关键配置验证通过！</b></color>");
            Debug.Log("如果面板仍然无法显示数据，请按Space键打开面板，然后查看上方的详细日志。");
        }
        else
        {
            Debug.LogError("❌ <color=red><b>发现配置问题！请根据上方的错误提示修复。</b></color>");
        }
        Debug.Log("========================================");
    }

    /// <summary>
    /// 验证SpiritPanelController
    /// </summary>
    private bool ValidateSpiritPanelController()
    {
        Debug.Log("\n[步骤1] 检查 SpiritPanelController...");

        var controller = FindObjectOfType<SpiritPanelController>();
        if (controller == null)
        {
            Debug.LogError("❌ 场景中没有找到 SpiritPanelController！");
            Debug.LogError("   解决方法：在场景中创建GameObject并添加SpiritPanelController组件");
            return false;
        }

        Debug.Log($"✅ 找到 SpiritPanelController: {controller.gameObject.name}");

        // 使用反射检查私有字段
        var controllerType = controller.GetType();
        var spiritPanelField = controllerType.GetField("spiritPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerDataField = controllerType.GetField("playerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var spiritPanel = spiritPanelField?.GetValue(controller) as UI_SpiritPanel;
        var playerData = playerDataField?.GetValue(controller) as PlayerData;

        if (spiritPanel == null)
        {
            Debug.LogError("❌ SpiritPanelController.spiritPanel 未赋值！");
            Debug.LogError("   解决方法：在Inspector中将UI_SpiritPanel对象拖入Spirit Panel字段");
            return false;
        }

        Debug.Log($"✅ spiritPanel 已赋值: {spiritPanel.gameObject.name}");

        if (playerData == null)
        {
            Debug.LogWarning("⚠️ SpiritPanelController.playerData 未赋值");
            Debug.LogWarning("   这是可选的，但如果你想要自动初始化，请在Inspector中赋值PlayerData");
        }
        else
        {
            Debug.Log($"✅ playerData 已赋值: {playerData.name}");
        }

        return true;
    }

    /// <summary>
    /// 验证UI_SpiritPanel
    /// </summary>
    private bool ValidateUISpiritPanel()
    {
        Debug.Log("\n[步骤2] 检查 UI_SpiritPanel...");

        var panel = FindObjectOfType<UI_SpiritPanel>();
        if (panel == null)
        {
            Debug.LogError("❌ 场景中没有找到 UI_SpiritPanel！");
            return false;
        }

        Debug.Log($"✅ 找到 UI_SpiritPanel: {panel.gameObject.name}");

        // 使用反射检查私有字段
        var panelType = panel.GetType();
        var panelRootField = panelType.GetField("panelRoot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var deployedContainerField = panelType.GetField("deployedSlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ownedContainerField = panelType.GetField("ownedSlotsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerField = panelType.GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var panelRoot = panelRootField?.GetValue(panel) as GameObject;
        var deployedContainer = deployedContainerField?.GetValue(panel) as Transform;
        var ownedContainer = ownedContainerField?.GetValue(panel) as Transform;
        var player = playerField?.GetValue(panel) as Player;

        bool valid = true;

        if (panelRoot == null)
        {
            Debug.LogError("❌ UI_SpiritPanel.panelRoot 未赋值！");
            Debug.LogError("   解决方法：在Inspector中将面板根GameObject拖入Panel Root字段");
            valid = false;
        }
        else
        {
            Debug.Log($"✅ panelRoot 已赋值: {panelRoot.name}");
            Debug.Log($"   panelRoot.activeSelf = {panelRoot.activeSelf}");
        }

        if (deployedContainer == null)
        {
            Debug.LogError("❌ UI_SpiritPanel.deployedSlotsContainer 未赋值！");
            Debug.LogError("   解决方法：在Inspector中将已出场槽位容器拖入Deployed Slots Container字段");
            valid = false;
        }
        else
        {
            Debug.Log($"✅ deployedSlotsContainer 已赋值: {deployedContainer.name}");
            Debug.Log($"   容器中槽位数量: {deployedContainer.childCount}");
        }

        if (ownedContainer == null)
        {
            Debug.LogError("❌ UI_SpiritPanel.ownedSlotsContainer 未赋值！");
            Debug.LogError("   解决方法：在Inspector中将拥有槽位容器拖入Owned Slots Container字段");
            valid = false;
        }
        else
        {
            Debug.Log($"✅ ownedSlotsContainer 已赋值: {ownedContainer.name}");
            Debug.Log($"   容器中槽位数量: {ownedContainer.childCount}");
        }

        if (player == null)
        {
            Debug.LogWarning("⚠️ UI_SpiritPanel.player 为null");
            Debug.LogWarning("   这可能是因为：");
            Debug.LogWarning("   1. SpiritPanelController尚未调用SetPlayer()");
            Debug.LogWarning("   2. PlayerData未正确配置");
            Debug.LogWarning("   请确保游戏启动时看到 [SpiritPanelController] Auto-initialization 日志");
        }
        else
        {
            Debug.Log($"✅ player 已绑定");
            Debug.Log($"   Player拥有Spirit数量: {player.GetAllSpirits().Count}");
            Debug.Log($"   Player已出场Spirit数量: {player.GetDeployedSpirits().Count}");
        }

        return valid;
    }

    /// <summary>
    /// 验证PlayerData
    /// </summary>
    private bool ValidatePlayerData()
    {
        Debug.Log("\n[步骤3] 检查 PlayerData...");

        var controller = FindObjectOfType<SpiritPanelController>();
        if (controller == null)
        {
            Debug.LogWarning("⚠️ 跳过PlayerData检查（没有找到SpiritPanelController）");
            return true;
        }

        var controllerType = controller.GetType();
        var playerDataField = controllerType.GetField("playerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerData = playerDataField?.GetValue(controller) as PlayerData;

        if (playerData == null)
        {
            Debug.LogWarning("⚠️ PlayerData 未配置");
            Debug.LogWarning("   如果你想使用自动初始化功能，请在SpiritPanelController的Inspector中配置PlayerData");
            return true; // 这不是致命错误
        }

        Debug.Log($"✅ 找到 PlayerData: {playerData.name}");

        // 检查PlayerData的Spirit数组
        if (playerData.OwnedSpirits == null || playerData.OwnedSpirits.Length == 0)
        {
            Debug.LogError("❌ PlayerData.OwnedSpirits 为空！");
            Debug.LogError("   解决方法：");
            Debug.LogError("   1. 在Project窗口选中你的PlayerData资源");
            Debug.LogError("   2. 在Inspector中找到Owned Spirits数组");
            Debug.LogError("   3. 添加元素并拖入SpiritData资源");
            return false;
        }

        Debug.Log($"✅ OwnedSpirits 包含 {playerData.OwnedSpirits.Length} 个Spirit:");
        for (int i = 0; i < playerData.OwnedSpirits.Length; i++)
        {
            var spirit = playerData.OwnedSpirits[i];
            if (spirit == null)
            {
                Debug.LogError($"   ❌ OwnedSpirits[{i}] 为 null！");
            }
            else
            {
                Debug.Log($"   [{i}] {spirit.DisplayName} (HP:{spirit.MaxHP} ATK:{spirit.Damage})");
            }
        }

        if (playerData.DeployedSpirits != null && playerData.DeployedSpirits.Length > 0)
        {
            Debug.Log($"✅ DeployedSpirits 包含 {playerData.DeployedSpirits.Length} 个Spirit:");
            for (int i = 0; i < playerData.DeployedSpirits.Length; i++)
            {
                var spirit = playerData.DeployedSpirits[i];
                if (spirit == null)
                {
                    Debug.LogWarning($"   ⚠️ DeployedSpirits[{i}] 为 null");
                }
                else
                {
                    Debug.Log($"   [{i}] {spirit.DisplayName}");
                }
            }
        }
        else
        {
            Debug.Log("   DeployedSpirits 为空（这是可以的）");
        }

        return true;
    }

    /// <summary>
    /// 验证Player运行时实例
    /// </summary>
    private bool ValidatePlayerInstance()
    {
        Debug.Log("\n[步骤4] 检查 Player 运行时实例...");

        var panel = FindObjectOfType<UI_SpiritPanel>();
        if (panel == null)
        {
            Debug.LogWarning("⚠️ 跳过Player实例检查（没有找到UI_SpiritPanel）");
            return true;
        }

        var panelType = panel.GetType();
        var playerField = panelType.GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var player = playerField?.GetValue(panel) as Player;

        if (player == null)
        {
            Debug.LogError("❌ Player 实例未创建或未绑定！");
            Debug.LogError("   这意味着：");
            Debug.LogError("   1. SpiritPanelController的Start()可能没有成功执行");
            Debug.LogError("   2. PlayerData可能未正确配置");
            Debug.LogError("   3. PlayerData.CreatePlayer()可能失败");
            Debug.LogError("   请查看上方日志中是否有 [SpiritPanelController] Auto-initializing 的消息");
            return false;
        }

        Debug.Log("✅ Player 实例已创建并绑定");

        var allSpirits = player.GetAllSpirits();
        var deployedSpirits = player.GetDeployedSpirits();

        Debug.Log($"   拥有的Spirit数量: {allSpirits.Count}");
        Debug.Log($"   已出场的Spirit数量: {deployedSpirits.Count}");
        Debug.Log($"   剩余出场槽位: {player.GetRemainingDeploySlots()}");

        if (allSpirits.Count == 0)
        {
            Debug.LogError("❌ Player没有任何Spirit！");
            Debug.LogError("   这意味着PlayerData.CreatePlayer()没有正确添加Spirit");
            Debug.LogError("   请检查PlayerData.OwnedSpirits数组是否有数据");
            return false;
        }

        Debug.Log("   所有Spirit列表:");
        for (int i = 0; i < allSpirits.Count; i++)
        {
            var spirit = allSpirits[i];
            bool isDeployed = player.IsDeployed(spirit);
            string status = isDeployed ? "[已出场]" : "[未出场]";
            Debug.Log($"   {status} {spirit.DisplayName}");
        }

        return true;
    }

    /// <summary>
    /// 验证输入系统
    /// </summary>
    private void ValidateInputSystem()
    {
        Debug.Log("\n[步骤5] 检查输入系统...");

        // 检查是否有输入组件
        var spiritPanelInput = FindObjectOfType<SpiritPanelInput>();
        var gameManager = FindObjectOfType<GameManagerExample>();

        if (spiritPanelInput == null && gameManager == null)
        {
            Debug.LogWarning("⚠️ 没有找到输入处理组件！");
            Debug.LogWarning("   你可以：");
            Debug.LogWarning("   1. 添加 SpiritPanelInput 组件处理快捷键");
            Debug.LogWarning("   2. 添加 GameManagerExample 组件（内置Space键支持）");
            Debug.LogWarning("   3. 手动调用 SpiritPanelController.ShowPanel()");
        }
        else
        {
            if (spiritPanelInput != null)
            {
                Debug.Log($"✅ 找到 SpiritPanelInput: {spiritPanelInput.gameObject.name}");
            }
            if (gameManager != null)
            {
                Debug.Log($"✅ 找到 GameManagerExample: {gameManager.gameObject.name}");
                Debug.Log("   GameManagerExample 使用 Space 键打开面板");
            }
        }
    }

    /// <summary>
    /// 在Inspector中添加手动验证按钮
    /// </summary>
    [ContextMenu("立即验证配置")]
    private void ManualValidate()
    {
        ValidateSetup();
    }
}
