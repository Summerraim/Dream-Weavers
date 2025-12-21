using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 战斗 UI 视图，由 `BattleController` 管理。负责展示玩家与敌方的头像、血量/蓝量和两个交互按钮。
/// </summary>
public class UI_BattleView : MonoBehaviour
{
    [Header("Main Battle Panel")]
    [SerializeField]
    [Tooltip("战斗主面板，包含所有战斗UI组件。Bind时自动激活，Unbind时自动隐藏。")]
    private GameObject battlePanel;

    [Header("Background")]
    [SerializeField]
    private Image backgroundImage;

    [Header("References")]
    [SerializeField]
    private Button endTurnButton;

    [Header("Skill Buttons")]
    [SerializeField]
    private Button skillButton1;

    [SerializeField]
    private Button skillButton2;

    [SerializeField]
    private Button skillButton3;

    [Header("Unit Images")]
    [SerializeField]
    private Image spiritImage1;

    [SerializeField]
    private Image spiritImage2;

    [SerializeField]
    private Image enemyImage1;

    [SerializeField]
    private Image enemyImage2;

    [Header("Unit Names")]
    [SerializeField]
    private TMP_Text spiritNameText;

    [SerializeField]
    private TMP_Text enemyNameText;

    [Header("Unit Stats")]
    [SerializeField]
    private ImageBar spiritHpBar;

    [SerializeField]
    private TMP_Text spiritHpText;

    [SerializeField]
    private ImageBar spiritMpBar;

    [SerializeField]
    private TMP_Text spiritMpText;

    [SerializeField]
    private ImageBar spiritShieldBar;

    [SerializeField]
    private TMP_Text spiritShieldText;

    [SerializeField]
    private ImageBar enemyHpBar;

    [SerializeField]
    private TMP_Text enemyHpText;

    [SerializeField]
    private ImageBar enemyMpBar;

    [SerializeField]
    private TMP_Text enemyMpText;

    [SerializeField]
    private ImageBar enemyShieldBar;

    [SerializeField]
    private TMP_Text enemyShieldText;

    [Header("Debug / Info")]
    [SerializeField]
    private TMP_Text turnText;

    [Header("Synergy Display")]
    [SerializeField]
    private GameObject synergySlotPrefab;

    [SerializeField]
    private Transform spiritSynergiesContainer;

    [SerializeField]
    private int maxSynergiesDisplay = 6; // 最多显示的羁绊数量

    [Header("Spirit Switcher")]
    [SerializeField]
    private GameObject spiritSlotPrefab;

    [SerializeField]
    private Transform spiritSlotsContainer;

    [SerializeField]
    private Button spiritSwitcherToggleButton;

    [SerializeField]
    private GameObject spiritSwitcherPanel;

    [Header("Effect Display")]
    [SerializeField]
    private GameObject effectSlotPrefab;

    [SerializeField]
    private Transform playerEffectsContainer;

    [SerializeField]
    private GameObject playerEffectsPanel;

    [SerializeField]
    private Transform enemyEffectsContainer;

    [SerializeField]
    private GameObject enemyEffectsPanel;

    [SerializeField]
    private bool hideEmptyEffectPanels = true;

    [SerializeField]
    private int maxEffectsPerUnit = 10; // 每个单位最多显示的Effect数量

    [Header("Enemy Death Panel")]
    [SerializeField]
    private GameObject enemyDeathPanel;

    [SerializeField]
    private Transform enemyDeathSlotContainer;

    [SerializeField]
    [Tooltip("战斗胜利后点击继续/离开房间的按钮")]
    private Button continueButton;

    [Header("Battle Lose Panel")]
    [SerializeField]
    [Tooltip("战斗失败面板（所有精灵死亡时显示）")]
    private GameObject losePanel;

    [SerializeField]
    [Tooltip("战斗失败后的重试按钮")]
    private Button retryButton;

    [Header("Capture UI")]
    [SerializeField]
    private GameObject capturePanel;

    [SerializeField]
    private TMP_Text captureResultText;

    private BattleController controller;
    private BattleModel model;
    private List<SynergySlot> spiritSynergySlots = new List<SynergySlot>();
    private SpiritSlot[] spiritSlots;
    private List<EffectSlot> playerEffectSlots = new List<EffectSlot>();
    private List<EffectSlot> enemyEffectSlots = new List<EffectSlot>();

    // 道具目标选择模式
    private bool isSelectingItemTarget = false;

    private void Awake()
    {
        // 确保战斗面板初始状态为隐藏
        if (battlePanel != null)
        {
            battlePanel.SetActive(false);
        }

        // 确保失败面板初始状态为隐藏
        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示战斗面板
    /// </summary>
    public void ShowBattlePanel()
    {
        if (battlePanel != null)
        {
            battlePanel.SetActive(true);
            Debug.Log("[UI_BattleView] ShowBattlePanel called");
        }
    }

    /// <summary>
    /// 隐藏战斗面板
    /// </summary>
    public void HideBattlePanel()
    {
        if (battlePanel != null)
        {
            battlePanel.SetActive(false);
            Debug.Log("[UI_BattleView] HideBattlePanel called");
        }
    }

    public void Bind(BattleController ctrl, BattleModel m)
    {
        Debug.Log($"[UI_BattleView] Bind called - ctrl={(ctrl != null ? "valid" : "null")}, model={(m != null ? "valid" : "null")}, battlePanel={(battlePanel != null ? battlePanel.name : "NULL")}");
        Debug.Log($"[UI_BattleView] UI_BattleView GameObject: {gameObject.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
        
        Unbind();

        controller = ctrl;
        model = m;

        // 激活战斗主面板
        if (battlePanel != null)
        {
            // 先检查并激活父级链
            Transform current = battlePanel.transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.Log($"[UI_BattleView] Activating parent: {current.gameObject.name}");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            battlePanel.SetActive(true);
            Debug.Log($"[UI_BattleView] Battle panel activated: {battlePanel.name}, activeSelf={battlePanel.activeSelf}, activeInHierarchy={battlePanel.activeInHierarchy}");
            
            // 如果仍然不可见，输出父级链状态
            if (!battlePanel.activeInHierarchy)
            {
                Debug.LogError($"[UI_BattleView] battlePanel still not visible! Checking parent chain:");
                Transform parent = battlePanel.transform.parent;
                while (parent != null)
                {
                    Debug.LogError($"  -> {parent.name}: activeSelf={parent.gameObject.activeSelf}, activeInHierarchy={parent.gameObject.activeInHierarchy}");
                    parent = parent.parent;
                }
            }
        }
        else
        {
            Debug.LogError("[UI_BattleView] battlePanel is NULL! Cannot show battle UI. Please assign battlePanel in Inspector.");
        }

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);

        if (skillButton1 != null)
            skillButton1.onClick.AddListener(() => OnSkillClicked(0));

        if (skillButton2 != null)
            skillButton2.onClick.AddListener(() => OnSkillClicked(1));

        if (skillButton3 != null)
            skillButton3.onClick.AddListener(() => OnSkillClicked(2));

        // 初始化Spirit切换器
        if (spiritSwitcherToggleButton != null)
            spiritSwitcherToggleButton.onClick.AddListener(ToggleSpiritSwitcherPanel);

        // 初始化Spirit切换器面板（默认隐藏）
        if (spiritSwitcherPanel != null)
            spiritSwitcherPanel.SetActive(false);

        // 初始化敌人死亡面板（默认隐藏）
        if (enemyDeathPanel != null)
            enemyDeathPanel.SetActive(false);

        // 初始化失败面板（默认隐藏）
        if (losePanel != null)
            losePanel.SetActive(false);

        // 绑定继续按钮（战斗胜利后离开房间）
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueButtonClicked);

        // 绑定重试按钮（战斗失败后重试）
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);

        // 初始化捕捉UI面板（默认隐藏）
        if (capturePanel != null)
            capturePanel.SetActive(false);

        // 初始化羁绊槽位
        InitializeSynergySlots();

        // 初始化Spirit槽位
        InitializeSpiritSlots();

        // 初始化Effect槽位
        InitializeEffectSlots();

        Refresh();
    }

    public void Unbind()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);

        if (skillButton1 != null)
            skillButton1.onClick.RemoveAllListeners();

        if (skillButton2 != null)
            skillButton2.onClick.RemoveAllListeners();

        if (skillButton3 != null)
            skillButton3.onClick.RemoveAllListeners();

        if (spiritSwitcherToggleButton != null)
            spiritSwitcherToggleButton.onClick.RemoveListener(ToggleSpiritSwitcherPanel);

        // 移除继续按钮监听
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);

        // 移除重试按钮监听
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryButtonClicked);

        // 清空羁绊槽位
        ClearSynergySlots();

        // 清空Effect槽位
        ClearAllEffectSlots();

        // 隐藏战斗主面板
        if (battlePanel != null)
        {
            battlePanel.SetActive(false);
            Debug.Log("[UI_BattleView] Battle panel deactivated");
        }

        controller = null;
        model = null;
    }

    private void OnEndTurnClicked()
    {
        Debug.Log("UI: End Turn button clicked.");
        if (controller != null)
        {
            controller.EndPlayerTurn();
            return;
        }

        // 回退：尝试在场景中查找 BattleController 并调用（帮助快速排查绑定问题）
        var fallback = FindObjectOfType<BattleController>();
        if (fallback != null)
        {
            Debug.Log(
                "UI: controller is null, fallback to scene BattleController for EndPlayerTurn"
            );
            fallback.EndPlayerTurn();
            return;
        }

        Debug.LogWarning("UI: EndTurn clicked but no BattleController bound or found in scene.");
    }

    private void OnSkillClicked(int skillIndex)
    {
        Debug.Log($"UI: Skill button {skillIndex + 1} clicked.");
        if (controller != null)
        {
            controller.UsePlayerSkill(skillIndex);
            return;
        }

        var fallback = FindObjectOfType<BattleController>();
        if (fallback != null)
        {
            Debug.Log(
                $"UI: controller is null, fallback to scene BattleController for UsePlayerSkill({skillIndex})"
            );
            fallback.UsePlayerSkill(skillIndex);
            return;
        }

        Debug.LogWarning(
            $"UI: Skill {skillIndex} clicked but no BattleController bound or found in scene."
        );
    }

    /// <summary>
    /// 根据当前 `controller` / `model` 刷新视图显示。
    /// </summary>
    public void Refresh()
    {
        if (model == null || controller == null)
            return;

        var player = model.PlayerUnit;
        var enemy =
            (model.EnemyUnits != null && model.EnemyUnits.Count > 0)
                ? model.EnemyUnits[0]
                : controller.Enemy;
        // 头像：直接使用 IBattleUnit 的 Image 属性
        if (spiritImage1 != null && player != null)
        {
            if (player.Image != null)
                spiritImage1.sprite = player.Image;
        }

        if (enemyImage1 != null && enemy != null)
        {
            if (enemy.Image != null)
                enemyImage1.sprite = enemy.Image;
        }
        if (spiritImage2 != null && player != null)
        {
            if (player.Image != null)
            {
                spiritImage2.sprite = player.Image;
                AdjustSpiritImageSize(spiritImage2, player.DisplayName);
            }
        }

        if (enemyImage2 != null && enemy != null)
        {
            if (enemy.Image != null)
            {
                enemyImage2.sprite = enemy.Image;
                AdjustEnemyImageSize(enemyImage2, enemy.DisplayName);
            }
        }

        // 名称显示
        if (spiritNameText != null && player != null)
        {
            spiritNameText.text = player.DisplayName;
        }

        if (enemyNameText != null && enemy != null)
        {
            enemyNameText.text = enemy.DisplayName;
        }

        // 血量/蓝量：使用单位公开的属性，不直接依赖数据对象字段名
        if (spiritHpBar != null && player != null)
            spiritHpBar.Set(player.HP, player.MaxHP);

        if (spiritHpText != null && player != null)
            spiritHpText.text = $"{player.HP}/{player.MaxHP}";

        if (spiritMpBar != null && player != null)
            spiritMpBar.Set(player.Mana, player.MaxMana);

        if (spiritMpText != null && player != null)
            spiritMpText.text = $"{player.Mana}/{player.MaxMana}";

        // Spirit Shield
        if (player != null)
        {
            var shieldInfo = model.GetUnitShieldInfo(player);

            if (spiritShieldBar != null)
            {
                if (shieldInfo.max > 0)
                {
                    spiritShieldBar.Set(shieldInfo.current, shieldInfo.max);
                    spiritShieldBar.gameObject.SetActive(true);
                }
                else
                {
                    spiritShieldBar.gameObject.SetActive(false);
                }
            }

            if (spiritShieldText != null)
            {
                if (shieldInfo.max > 0)
                {
                    spiritShieldText.text = $"{shieldInfo.current}/{shieldInfo.max}";
                    spiritShieldText.gameObject.SetActive(true);
                }
                else
                {
                    spiritShieldText.gameObject.SetActive(false);
                }
            }
        }

        if (enemyHpBar != null && enemy != null)
            enemyHpBar.Set(enemy.HP, enemy.MaxHP);

        if (enemyHpText != null && enemy != null)
            enemyHpText.text = $"{enemy.HP}/{enemy.MaxHP}";

        if (enemyMpBar != null && enemy != null)
            enemyMpBar.Set(enemy.Mana, enemy.MaxMana);

        if (enemyMpText != null && enemy != null)
            enemyMpText.text = $"{enemy.Mana}/{enemy.MaxMana}";

        // Enemy Shield
        if (enemy != null)
        {
            var shieldInfo = model.GetUnitShieldInfo(enemy);

            if (enemyShieldBar != null)
            {
                if (shieldInfo.max > 0)
                {
                    enemyShieldBar.Set(shieldInfo.current, shieldInfo.max);
                    enemyShieldBar.gameObject.SetActive(true);
                }
                else
                {
                    enemyShieldBar.gameObject.SetActive(false);
                }
            }

            if (enemyShieldText != null)
            {
                if (shieldInfo.max > 0)
                {
                    enemyShieldText.text = $"{shieldInfo.current}/{shieldInfo.max}";
                    enemyShieldText.gameObject.SetActive(true);
                }
                else
                {
                    enemyShieldText.gameObject.SetActive(false);
                }
            }
        }

        if (turnText != null && model != null)
        {
            turnText.text = $"Turn: {model.CurrentTurn}";
        }

        // 更新技能按钮状态
        UpdateSkillButtons();

        // 更新羁绊显示
        UpdateSynergyDisplay();

        // 更新Spirit槽位显示
        RefreshSpiritSlots();

        // 更新Effect显示
        RefreshEffectDisplay();
    }

    /// <summary>
    /// 更新技能按钮的可用状态（根据冷却、蓝量等条件）
    /// </summary>
    private void UpdateSkillButtons()
    {
        if (model == null || model.PlayerUnit == null)
            return;

        var skills = model.PlayerUnit.GetSkills();
        UpdateSkillButton(skillButton1, 0, skills);
        UpdateSkillButton(skillButton2, 1, skills);
        UpdateSkillButton(skillButton3, 2, skills);
    }

    /// <summary>
    /// 更新单个技能按钮的状态
    /// </summary>
    private void UpdateSkillButton(Button button, int skillIndex, IReadOnlyList<ISkill> skills)
    {
        if (button == null)
            return;

        // 检查技能是否存在
        if (skills == null || skillIndex >= skills.Count)
        {
            button.interactable = false;
            UpdateButtonText(button, "---"); // 显示无技能
            return;
        }

        var skill = skills[skillIndex];
        if (skill == null)
        {
            button.interactable = false;
            UpdateButtonText(button, "---"); // 显示无技能
            return;
        }

        // 获取技能名称、描述和蓝耗信息
        string skillName = skill.DisplayName;
        string description = skill.Description;
        int manaCost = skill.ManaCost;

        // 检查使用次数限制
        if (model.IsSkillUsageLimitReached(skillIndex, skill))
        {
            button.interactable = false;
            int remainingUses = model.GetSkillRemainingUses(skillIndex, skill);
            UpdateButtonText(
                button,
                $"{skillName}\n{description}\n次数:0/{skill.MaxUsesPerBattle}"
            );
            return;
        }

        // 检查冷却
        if (model.IsSkillOnCooldown(skillIndex))
        {
            button.interactable = false;
            var cooldown = model.GetSkillCooldown(skillIndex);
            UpdateButtonText(button, $"{skillName}\n{description}\n冷却:{cooldown}");
            return;
        }

        // 检查蓝量
        if (model.PlayerUnit.Mana < manaCost)
        {
            button.interactable = false;
            UpdateButtonText(button, $"{skillName}\n{description}\n蓝耗:{manaCost}");
            return;
        }

        // 技能可用 - 显示剩余使用次数（如果有限制）
        button.interactable = true;
        if (skill.MaxUsesPerBattle > 0)
        {
            int remainingUses = model.GetSkillRemainingUses(skillIndex, skill);
            UpdateButtonText(
                button,
                $"{skillName}\n{description}\n蓝耗:{manaCost} | 次数:{remainingUses}/{skill.MaxUsesPerBattle}"
            );
        }
        else
        {
            UpdateButtonText(button, $"{skillName}\n{description}\n蓝耗:{manaCost}");
        }
    }

    /// <summary>
    /// 更新按钮显示的文本（如果按钮包含Text组件）
    /// </summary>
    private void UpdateButtonText(Button button, string text)
    {
        if (button == null)
            return;

        // 尝试查找按钮下的Text组件
        var textComponent = button.GetComponentInChildren<TMPro.TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
            return;
        }

        var legacyText = button.GetComponentInChildren<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }

    /// <summary>
    /// 初始化羁绊槽位对象池
    /// </summary>
    private void InitializeSynergySlots()
    {
        Debug.Log("[UI_BattleView] InitializeSynergySlots开始");

        // 清空旧槽位
        ClearSynergySlots();

        if (spiritSynergiesContainer == null)
        {
            Debug.LogWarning("[UI_BattleView] spiritSynergiesContainer为null，无法初始化羁绊槽位");
            return;
        }

        // 预创建槽位对象
        for (int i = 0; i < maxSynergiesDisplay; i++)
        {
            GameObject slotObj;

            if (synergySlotPrefab != null)
            {
                slotObj = Instantiate(synergySlotPrefab, spiritSynergiesContainer);
            }
            else
            {
                // 如果没有预制体，创建默认槽位
                slotObj = CreateDefaultSynergySlot();
                slotObj.transform.SetParent(spiritSynergiesContainer, false);
            }

            var slot = slotObj.GetComponent<SynergySlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<SynergySlot>();
            }

            slot.Clear(); // 初始时隐藏
            spiritSynergySlots.Add(slot);
        }

        Debug.Log(
            $"[UI_BattleView] InitializeSynergySlots完成，创建{spiritSynergySlots.Count}个槽位"
        );
    }

    /// <summary>
    /// 创建默认羁绊槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultSynergySlot()
    {
        GameObject slotObj = new GameObject("SynergySlot");

        // 添加Image组件作为背景
        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // 设置RectTransform
        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        return slotObj;
    }

    /// <summary>
    /// 清空羁绊槽位
    /// </summary>
    private void ClearSynergySlots()
    {
        foreach (var slot in spiritSynergySlots)
        {
            if (slot != null)
            {
                slot.Clear();
            }
        }
    }

    /// <summary>
    /// 更新羁绊显示
    /// </summary>
    private void UpdateSynergyDisplay()
    {
        if (model == null || model.PlayerUnit == null)
        {
            ClearSynergySlots();
            return;
        }

        // 获取PlayerUnit作为Spirit
        var player = model.PlayerUnit;
        if (player is Spirit spirit)
        {
            var synergies = spirit.Synergies;
            Debug.Log(
                $"[UI_BattleView] UpdateSynergyDisplay: {spirit.DisplayName}, 羁绊数量={synergies?.Count ?? 0}"
            );

            // 更新槽位显示
            int synergyIndex = 0;
            if (synergies != null)
            {
                for (int i = 0; i < synergies.Count && synergyIndex < spiritSynergySlots.Count; i++)
                {
                    var synergy = synergies[i];
                    if (synergy != null && synergy.Synergy != null)
                    {
                        Debug.Log(
                            $"[UI_BattleView] 设置羁绊槽位{synergyIndex}: {synergy.Synergy.DisplayName}"
                        );
                        spiritSynergySlots[synergyIndex].SetSynergy(synergy);
                        synergyIndex++;
                    }
                }
            }

            Debug.Log($"[UI_BattleView] 总共设置了{synergyIndex}个羁绊槽位");

            // 清空未使用的槽位
            for (int i = synergyIndex; i < spiritSynergySlots.Count; i++)
            {
                spiritSynergySlots[i].Clear();
            }
        }
        else
        {
            Debug.Log(
                $"[UI_BattleView] PlayerUnit不是Spirit类型，无法显示羁绊: {player?.GetType().Name}"
            );
            ClearSynergySlots();
        }
    }

    // ========== Spirit Switcher功能 ==========

    /// <summary>
    /// 初始化Spirit槽位
    /// </summary>
    private void InitializeSpiritSlots()
    {
        if (controller == null || spiritSlotsContainer == null)
        {
            Debug.LogWarning("[UI_BattleView] 无法初始化Spirit槽位");
            return;
        }

        // 清除现有槽位
        foreach (Transform child in spiritSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        // 创建6个槽位
        spiritSlots = new SpiritSlot[6];

        // 优先从PlayerManager获取最新的部署Spirit列表
        List<SpiritData> deployedSpirits = null;
        if (PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayer != null)
        {
            deployedSpirits = PlayerManager.Instance.GetDeployedSpirits();
            Debug.Log($"[UI_BattleView] 从PlayerManager获取部署Spirit列表: {deployedSpirits.Count} 个");
        }
        else
        {
            // 降级方案：从BattleController获取
            deployedSpirits = controller.GetDeployedSpirits();
            Debug.Log($"[UI_BattleView] 从BattleController获取部署Spirit列表: {(deployedSpirits != null ? deployedSpirits.Count : 0)} 个");
        }

        if (deployedSpirits == null)
        {
            Debug.LogWarning("[UI_BattleView] 无法获取部署Spirit列表");
            deployedSpirits = new List<SpiritData>();
        }

        for (int i = 0; i < 6; i++)
        {
            GameObject slotObj;

            // 如果有预制体，使用预制体；否则创建简单的按钮
            if (spiritSlotPrefab != null)
            {
                slotObj = Instantiate(spiritSlotPrefab, spiritSlotsContainer);
            }
            else
            {
                slotObj = CreateDefaultSpiritSlot();
                slotObj.transform.SetParent(spiritSlotsContainer, false);
            }

            slotObj.SetActive(true);

            var slot = slotObj.GetComponent<SpiritSlot>();
            if (slot == null)
                slot = slotObj.AddComponent<SpiritSlot>();

            slot.enabled = true;

            // 设置槽位数据
            if (i < deployedSpirits.Count && deployedSpirits[i] != null)
            {
                slot.Initialize(i, deployedSpirits[i], OnSpiritSlotClicked);
                Debug.Log($"[UI_BattleView] Spirit槽位 {i} 初始化: {deployedSpirits[i].DisplayName}");
            }
            else
            {
                slot.Initialize(i, null, OnSpiritSlotClicked);
                Debug.Log($"[UI_BattleView] Spirit槽位 {i} 初始化: 空槽位");
            }

            spiritSlots[i] = slot;
        }

        RefreshSpiritSlots();
    }

    /// <summary>
    /// 创建默认Spirit槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultSpiritSlot()
    {
        GameObject slotObj = new GameObject("SpiritSlot");

        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        slotObj.AddComponent<Button>();

        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        return slotObj;
    }

    /// <summary>
    /// 刷新所有Spirit槽位的状态
    /// </summary>
    public void RefreshSpiritSlots()
    {
        if (spiritSlots == null || controller == null)
            return;

        int currentIndex = controller.GetCurrentSpiritIndex();

        for (int i = 0; i < spiritSlots.Length; i++)
        {
            if (spiritSlots[i] != null)
            {
                spiritSlots[i].SetSelected(i == currentIndex);

                bool isAlive = controller.IsSpiritAlive(i);
                var runtimeData = controller.GetSpiritRuntimeData(i);

                spiritSlots[i]
                    .UpdateStatus(
                        runtimeData.CurrentHP,
                        runtimeData.MaxHP,
                        runtimeData.CurrentMP,
                        runtimeData.MaxMP,
                        isAlive
                    );
            }
        }
    }

    /// <summary>
    /// Spirit槽位点击回调
    /// </summary>
    private void OnSpiritSlotClicked(int slotIndex)
    {
        if (controller == null)
            return;

        Debug.Log(
            $"UI_BattleView: Spirit Slot {slotIndex} clicked, isSelectingItemTarget={isSelectingItemTarget}"
        );

        // 如果是道具目标选择模式
        if (isSelectingItemTarget)
        {
            // 通知BattleController选择了目标
            controller.OnSpiritSelectedAsItemTarget(slotIndex);

            // 重置状态
            isSelectingItemTarget = false;

            Debug.Log($"UI_BattleView: Spirit {slotIndex} selected as item target");
            return;
        }

        // 正常的Spirit切换逻辑
        bool success = controller.SwitchToSpirit(slotIndex);

        if (success)
        {
            RefreshSpiritSlots();
            Debug.Log($"UI_BattleView: Successfully switched to Spirit {slotIndex}");

            HideSpiritSwitcherPanel();
            controller.EndPlayerTurn();
        }
        else
        {
            Debug.LogWarning($"UI_BattleView: Failed to switch to Spirit {slotIndex}");
        }
    }

    /// <summary>
    /// 切换Spirit切换器面板显示/隐藏
    /// </summary>
    public void ToggleSpiritSwitcherPanel()
    {
        if (spiritSwitcherPanel != null)
        {
            bool isActive = !spiritSwitcherPanel.activeSelf;
            spiritSwitcherPanel.SetActive(isActive);

            if (isActive)
            {
                // 打开面板时重新初始化槽位（确保显示最新的Spirit列表）
                InitializeSpiritSlots();
            }
        }
    }

    /// <summary>
    /// 显示Spirit切换器面板
    /// </summary>
    public void ShowSpiritSwitcherPanel()
    {
        if (spiritSwitcherPanel != null)
        {
            spiritSwitcherPanel.SetActive(true);
            // 重新初始化槽位（确保显示最新的Spirit列表）
            InitializeSpiritSlots();
        }
    }

    /// <summary>
    /// 隐藏Spirit切换器面板
    /// </summary>
    public void HideSpiritSwitcherPanel()
    {
        if (spiritSwitcherPanel != null)
        {
            spiritSwitcherPanel.SetActive(false);
        }

        // 重置道具目标选择状态
        isSelectingItemTarget = false;
    }

    /// <summary>
    /// 显示Spirit切换器面板用于选择道具目标
    /// </summary>
    public void ShowSpiritSwitcherForItemTarget()
    {
        isSelectingItemTarget = true;

        if (spiritSwitcherPanel != null)
        {
            spiritSwitcherPanel.SetActive(true);
            RefreshSpiritSlots();
        }

        Debug.Log("UI_BattleView: Showing Spirit Switcher for item target selection");
    }

    // ========== Effect Display功能 ==========

    /// <summary>
    /// 初始化Effect槽位对象池
    /// </summary>
    private void InitializeEffectSlots()
    {
        Debug.Log("[UI_BattleView] InitializeEffectSlots开始");

        // 初始化面板状态
        if (playerEffectsPanel != null && hideEmptyEffectPanels)
            playerEffectsPanel.SetActive(false);

        if (enemyEffectsPanel != null && hideEmptyEffectPanels)
            enemyEffectsPanel.SetActive(false);

        // 预创建槽位对象
        CreateEffectSlotPool(playerEffectsContainer, playerEffectSlots, "Player");
        CreateEffectSlotPool(enemyEffectsContainer, enemyEffectSlots, "Enemy");

        Debug.Log(
            $"[UI_BattleView] InitializeEffectSlots完成: 玩家槽位数={playerEffectSlots.Count}, 敌人槽位数={enemyEffectSlots.Count}"
        );
    }

    /// <summary>
    /// 创建Effect槽位池
    /// </summary>
    private void CreateEffectSlotPool(
        Transform container,
        List<EffectSlot> slotList,
        string poolName
    )
    {
        Debug.Log($"[UI_BattleView] CreateEffectSlotPool开始: {poolName}");

        if (container == null)
        {
            Debug.LogWarning($"[UI_BattleView] {poolName} effects container为null");
            return;
        }

        for (int i = 0; i < maxEffectsPerUnit; i++)
        {
            GameObject slotObj;

            if (effectSlotPrefab != null)
            {
                slotObj = Instantiate(effectSlotPrefab, container);
            }
            else
            {
                slotObj = CreateDefaultEffectSlot();
                slotObj.transform.SetParent(container, false);
            }

            var slot = slotObj.GetComponent<EffectSlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<EffectSlot>();
            }

            slot.Clear();
            slotList.Add(slot);
        }

        Debug.Log(
            $"[UI_BattleView] CreateEffectSlotPool完成: {poolName}, 成功创建{slotList.Count}个槽位"
        );
    }

    /// <summary>
    /// 创建默认Effect槽位（如果没有提供预制体）
    /// </summary>
    private GameObject CreateDefaultEffectSlot()
    {
        GameObject slotObj = new GameObject("EffectSlot");

        var image = slotObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        var rectTransform = slotObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        return slotObj;
    }

    /// <summary>
    /// 刷新所有Effect显示
    /// </summary>
    public void RefreshEffectDisplay()
    {
        if (model == null)
        {
            Debug.LogWarning("[UI_BattleView] RefreshEffectDisplay: model is null!");
            return;
        }

        Debug.Log("[UI_BattleView] RefreshEffectDisplay开始");

        // 刷新玩家Effect
        if (model.PlayerUnit != null)
        {
            Debug.Log($"[UI_BattleView] 刷新玩家Effect: {model.PlayerUnit.DisplayName}");
            RefreshUnitEffects(model.PlayerUnit, playerEffectSlots, playerEffectsPanel);
        }
        else
        {
            ClearEffectSlots(playerEffectSlots, playerEffectsPanel);
        }

        // 刷新敌人Effect
        if (model.EnemyUnits != null && model.EnemyUnits.Count > 0)
        {
            var enemy = model.EnemyUnits[0];
            Debug.Log($"[UI_BattleView] 刷新敌人Effect: {enemy.DisplayName}");
            RefreshUnitEffects(enemy, enemyEffectSlots, enemyEffectsPanel);
        }
        else if (controller != null && controller.Enemy != null)
        {
            Debug.Log(
                $"[UI_BattleView] 刷新敌人Effect (from controller): {controller.Enemy.DisplayName}"
            );
            RefreshUnitEffects(controller.Enemy, enemyEffectSlots, enemyEffectsPanel);
        }
        else
        {
            ClearEffectSlots(enemyEffectSlots, enemyEffectsPanel);
        }

        Debug.Log("[UI_BattleView] RefreshEffectDisplay完成");
    }

    /// <summary>
    /// 刷新单个单位的Effect显示
    /// </summary>
    private void RefreshUnitEffects(IBattleUnit unit, List<EffectSlot> slotList, GameObject panel)
    {
        if (unit == null || model == null)
        {
            ClearEffectSlots(slotList, panel);
            return;
        }

        var buffs = model.GetBuffsForUnit(unit);

        Debug.Log(
            $"[UI_BattleView] RefreshUnitEffects: {unit.DisplayName}, Buff数量={(buffs != null ? buffs.Count : 0)}"
        );

        // 如果没有Effect且设置了隐藏空面板
        if ((buffs == null || buffs.Count == 0) && hideEmptyEffectPanels)
        {
            Debug.Log($"[UI_BattleView] 没有Buff，隐藏面板");
            ClearEffectSlots(slotList, panel);
            if (panel != null)
                panel.SetActive(false);
            return;
        }

        // 显示面板
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log($"[UI_BattleView] 显示面板: {panel.name}");
        }

        // 更新槽位显示
        int effectIndex = 0;
        if (buffs != null)
        {
            for (int i = 0; i < buffs.Count && effectIndex < slotList.Count; i++)
            {
                var buff = buffs[i];
                if (buff != null && !buff.IsExpired)
                {
                    Debug.Log($"[UI_BattleView] 设置Effect槽位{effectIndex}: {buff.DisplayName}");
                    slotList[effectIndex].SetEffect(buff);
                    effectIndex++;
                }
            }
        }

        Debug.Log($"[UI_BattleView] 总共设置了{effectIndex}个Effect槽位");

        // 清空未使用的槽位
        for (int i = effectIndex; i < slotList.Count; i++)
        {
            slotList[i].Clear();
        }
    }

    /// <summary>
    /// 清空指定Effect槽位列表
    /// </summary>
    private void ClearEffectSlots(List<EffectSlot> slotList, GameObject panel)
    {
        if (slotList != null)
        {
            foreach (var slot in slotList)
            {
                if (slot != null)
                    slot.Clear();
            }
        }

        if (panel != null && hideEmptyEffectPanels)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 清空所有Effect槽位
    /// </summary>
    private void ClearAllEffectSlots()
    {
        ClearEffectSlots(playerEffectSlots, playerEffectsPanel);
        ClearEffectSlots(enemyEffectSlots, enemyEffectsPanel);
    }

    /// <summary>
    /// 手动触发Effect刷新（供外部调用）
    /// </summary>
    public void UpdateEffects()
    {
        RefreshEffectDisplay();
    }

    /// <summary>
    /// 显示玩家Effect面板
    /// </summary>
    public void ShowPlayerEffects()
    {
        if (playerEffectsPanel != null)
            playerEffectsPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏玩家Effect面板
    /// </summary>
    public void HidePlayerEffects()
    {
        if (playerEffectsPanel != null)
            playerEffectsPanel.SetActive(false);
    }

    /// <summary>
    /// 显示敌人Effect面板
    /// </summary>
    public void ShowEnemyEffects()
    {
        if (enemyEffectsPanel != null)
            enemyEffectsPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏敌人Effect面板
    /// </summary>
    public void HideEnemyEffects()
    {
        if (enemyEffectsPanel != null)
            enemyEffectsPanel.SetActive(false);
    }

    /// <summary>
    /// 获取玩家当前Effect数量
    /// </summary>
    public int GetPlayerEffectCount()
    {
        if (model == null || model.PlayerUnit == null)
            return 0;

        var buffs = model.GetBuffsForUnit(model.PlayerUnit);
        return buffs != null ? buffs.Count : 0;
    }

    /// <summary>
    /// 获取敌人当前Effect数量
    /// </summary>
    public int GetEnemyEffectCount()
    {
        if (model == null)
            return 0;

        IBattleUnit enemy = null;
        if (model.EnemyUnits != null && model.EnemyUnits.Count > 0)
        {
            enemy = model.EnemyUnits[0];
        }
        else if (controller != null)
        {
            enemy = controller.Enemy;
        }

        if (enemy == null)
            return 0;

        var buffs = model.GetBuffsForUnit(enemy);
        return buffs != null ? buffs.Count : 0;
    }

    // ========== Enemy Death Panel功能 ==========

    /// <summary>
    /// 显示敌人死亡面板
    /// </summary>
    public void ShowEnemyDeathPanel()
    {
        if (enemyDeathPanel != null)
        {
            enemyDeathPanel.SetActive(true);
            Debug.Log("[UI_BattleView] 显示敌人死亡面板");
        }
    }

    /// <summary>
    /// 隐藏敌人死亡面板
    /// </summary>
    public void HideEnemyDeathPanel()
    {
        if (enemyDeathPanel != null)
        {
            enemyDeathPanel.SetActive(false);
            Debug.Log("[UI_BattleView] 隐藏敌人死亡面板");
        }
    }

    /// <summary>
    /// 显示战斗失败面板
    /// </summary>
    public void ShowLosePanel()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Debug.Log("[UI_BattleView] 显示战斗失败面板");
        }
    }

    /// <summary>
    /// 隐藏战斗失败面板
    /// </summary>
    public void HideLosePanel()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(false);
            Debug.Log("[UI_BattleView] 隐藏战斗失败面板");
        }
    }

    /// <summary>
    /// 继续按钮点击回调：战斗胜利后离开房间，触发路线选择
    /// </summary>
    private void OnContinueButtonClicked()
    {
        Debug.Log("[UI_BattleView] 继续按钮被点击，准备离开房间");

        // 隐藏敌人死亡面板
        HideEnemyDeathPanel();

        // 隐藏捕捉结果面板
        HideCapturePanel();

        // 隐藏主战斗面板
        HideBattlePanel();

        // 通知 RoomStateMachine 完成当前房间，触发路线选择
        if (RoomStateMachine_cza.Instance != null)
        {
            Debug.Log("[UI_BattleView] 通知 RoomStateMachine 完成房间");
            RoomStateMachine_cza.Instance.CompleteCurrentRoom();
        }
        else
        {
            Debug.LogWarning("[UI_BattleView] RoomStateMachine_cza.Instance 为 null，无法触发路线选择");
        }
    }

    /// <summary>
    /// 重试按钮点击回调：战斗失败后重新开始游戏
    /// </summary>
    private void OnRetryButtonClicked()
    {
        Debug.Log("[UI_BattleView] 重试按钮被点击");

        // 隐藏失败面板
        HideLosePanel();

        // 隐藏主战斗面板
        HideBattlePanel();

        // 重置时间缩放（以防在 GameOver 状态时被修改）
        Time.timeScale = 1f;

        // 方案1：回到主菜单（当前实现）
        // SceneManager.LoadScene(0);

        // 方案2：直接重新开始游戏（推荐）
        // 通过 GameManagerService 启动新游戏，会自动重置所有状态
        if (GameManagerService.Instance != null)
        {
            Debug.Log("[UI_BattleView] 通过 GameManagerService 启动新游戏");
            GameManagerService.Instance.StartNewGame();
        }
        else
        {
            // 降级方案：如果没有 GameManagerService，直接加载场景 0
            Debug.LogWarning("[UI_BattleView] GameManagerService 不存在，降级为加载场景 0");
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// 调整敌人图片大小（特殊处理）
    /// </summary>
    private void AdjustEnemyImageSize(Image enemyImage, string displayName)
    {
        if (enemyImage == null)
            return;

        var rectTransform = enemyImage.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        // 特殊处理：阿斯蒙蒂斯的图片大小设置为150x150
        if (displayName == "阿斯蒙蒂斯")
        {
            rectTransform.sizeDelta = new Vector2(150, 150);
            Debug.Log($"[UI_BattleView] 调整阿斯蒙蒂斯图片大小为150x150");
        }
        // 可以在这里添加其他特殊敌人的尺寸处理
        // else if (displayName == "其他Boss名称")
        // {
        //     rectTransform.sizeDelta = new Vector2(width, height);
        // }
        if (displayName == "霸王龙角斗士")
        {
            rectTransform.sizeDelta = new Vector2(100, 100);
            Debug.Log($"[UI_BattleView] 调整霸王龙角斗士图片大小为100x100");
        }
        if (displayName == "维京熊")
        {
            rectTransform.sizeDelta = new Vector2(98, 112);
            Debug.Log($"[UI_BattleView] 调整维京熊图片大小为98x112");
        }
        if (displayName == "派对熊")
        {
            rectTransform.sizeDelta = new Vector2(90, 103);
            Debug.Log($"[UI_BattleView] 调整派对熊图片大小为90x100");
        }
        if (displayName == "巴甫洛夫")
        {
            rectTransform.sizeDelta = new Vector2(105, 90);
            Debug.Log($"[UI_BattleView] 调整巴甫洛夫图片大小为105x90");
        }
        if (displayName == "默德拉斯")
        {
            rectTransform.sizeDelta = new Vector2(105, 90);
            Debug.Log($"[UI_BattleView] 调整默德拉斯图片大小为105x90");
        }
        if (displayName == "雕塑")
        {
            rectTransform.sizeDelta = new Vector2(85, 105);
            Debug.Log($"[UI_BattleView] 调整雕塑图片大小为85x105");
        }
        if (displayName == "飞鲸")
        {
            rectTransform.sizeDelta = new Vector2(90, 100);
            Debug.Log($"[UI_BattleView] 调整飞鲸图片大小为90x100");
        }
        if (displayName == "破坏者雷克")
        {
            rectTransform.sizeDelta = new Vector2(120, 115);
            Debug.Log($"[UI_BattleView] 调整破坏者雷克图片大小为120x115");
        }
        if (displayName == "鹿骑士")
        {
            rectTransform.sizeDelta = new Vector2(102, 90);
            Debug.Log($"[UI_BattleView] 调整鹿骑士图片大小为102x90");
        }
        if (displayName == "德芬斯")
        {
            rectTransform.sizeDelta = new Vector2(100, 90);
            Debug.Log($"[UI_BattleView] 调整德芬斯图片大小为100x90");
        }
        if (displayName == "眼球史莱姆")
        {
            rectTransform.sizeDelta = new Vector2(102, 75);
            Debug.Log($"[UI_BattleView] 调整眼球史莱姆图片大小为102x75");
        }
        if (displayName == "蘑菇枪兵")
        {
            rectTransform.sizeDelta = new Vector2(78, 103);
            Debug.Log($"[UI_BattleView] 调整蘑菇枪兵图片大小为78x103");
        }
    }

    /// <summary>
    /// 调整Spirit图片大小（特殊处理）
    /// </summary>
    private void AdjustSpiritImageSize(Image spiritImage, string displayName)
    {
        if (spiritImage == null)
            return;

        var rectTransform = spiritImage.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;
        if (displayName == "霸王龙角斗士")
        {
            rectTransform.sizeDelta = new Vector2(100, 100);
            Debug.Log($"[UI_BattleView] 调整霸王龙角斗士图片大小为100x100");
        }
        if (displayName == "维京熊")
        {
            rectTransform.sizeDelta = new Vector2(90, 110);
            Debug.Log($"[UI_BattleView] 调整维京熊图片大小为90x110");
        }
        if (displayName == "寒冰大炮手")
        {
            rectTransform.sizeDelta = new Vector2(88, 88);
            Debug.Log($"[UI_BattleView] 调整寒冰大炮手图片大小为88x88");
        }
        if (displayName == "森林大炮手")
        {
            rectTransform.sizeDelta = new Vector2(88, 88);
            Debug.Log($"[UI_BattleView] 调整森林大炮手图片大小为88x88");
        }
        if (displayName == "糖果大炮手")
        {
            rectTransform.sizeDelta = new Vector2(88, 88);
            Debug.Log($"[UI_BattleView] 调整糖果大炮手图片大小为88x88");
        }
        if (displayName == "派对熊")
        {
            rectTransform.sizeDelta = new Vector2(90, 102);
            Debug.Log($"[UI_BattleView] 调整派对熊图片大小为90x100");
        }
        if (displayName == "巴甫洛夫")
        {
            rectTransform.sizeDelta = new Vector2(100, 87);
            Debug.Log($"[UI_BattleView] 调整巴甫洛夫图片大小为100x87");
        }
        if (displayName == "默德拉斯")
        {
            rectTransform.sizeDelta = new Vector2(100, 87);
            Debug.Log($"[UI_BattleView] 调整默德拉斯图片大小为100x87");
        }
        if (displayName == "雕塑")
        {
            rectTransform.sizeDelta = new Vector2(85, 105);
            Debug.Log($"[UI_BattleView] 调整雕塑图片大小为85x105");
        }
        if (displayName == "飞鲸")
        {
            rectTransform.sizeDelta = new Vector2(90, 100);
            Debug.Log($"[UI_BattleView] 调整飞鲸图片大小为90x100");
        }
        if (displayName == "鹿骑士")
        {
            rectTransform.sizeDelta = new Vector2(100, 88);
            Debug.Log($"[UI_BattleView] 调整鹿骑士图片大小为100x88");
        }
        if (displayName == "鹿长官")
        {
            rectTransform.sizeDelta = new Vector2(100, 88);
            Debug.Log($"[UI_BattleView] 调整鹿长官图片大小为100x88");
        }
        if (displayName == "德芬斯")
        {
            rectTransform.sizeDelta = new Vector2(100, 90);
            Debug.Log($"[UI_BattleView] 调整德芬斯图片大小为100x90");
        }
        if (displayName == "眼球史莱姆")
        {
            rectTransform.sizeDelta = new Vector2(100, 76);
            Debug.Log($"[UI_BattleView] 调整眼球史莱姆图片大小为100x76");
        }
        if (displayName == "蘑菇枪兵")
        {
            rectTransform.sizeDelta = new Vector2(80, 95);
            Debug.Log($"[UI_BattleView] 调整蘑菇枪兵图片大小为80x95");
        }
        // 特殊处理：根据Spirit的DisplayName调整图片大小
        // 示例：如果Spirit名为"巨型守护者"，设置为180x180
        // if (displayName == "巨型守护者")
        // {
        //     rectTransform.sizeDelta = new Vector2(180, 180);
        //     Debug.Log($"[UI_BattleView] 调整{displayName}图片大小为180x180");
        // }

        // 可以在这里添加特殊Spirit的尺寸处理
        // else if (displayName == "其他Spirit名称")
        // {
        //     rectTransform.sizeDelta = new Vector2(width, height);
        // }
    }

    // ========== Capture UI功能 ==========

    /// <summary>
    /// 显示捕捉成功UI
    /// </summary>
    public void ShowCaptureSuccess(string spiritName)
    {
        if (capturePanel != null)
        {
            capturePanel.SetActive(true);
        }

        if (captureResultText != null)
        {
            captureResultText.text = $"捕捉成功！获得：{spiritName}";
            captureResultText.color = Color.green;
        }

        Debug.Log($"[UI_BattleView] 显示捕捉成功: {spiritName}");
    }

    /// <summary>
    /// 显示捕捉失败UI
    /// </summary>
    public void ShowCaptureFailed()
    {
        if (capturePanel != null)
        {
            capturePanel.SetActive(true);
        }

        if (captureResultText != null)
        {
            captureResultText.text = "捕捉失败...";
            captureResultText.color = Color.red;
        }

        Debug.Log("[UI_BattleView] 显示捕捉失败");
    }

    /// <summary>
    /// 隐藏捕捉UI面板
    /// </summary>
    public void HideCapturePanel()
    {
        if (capturePanel != null)
        {
            capturePanel.SetActive(false);
            Debug.Log("[UI_BattleView] 隐藏捕捉面板");
        }
    }
}
