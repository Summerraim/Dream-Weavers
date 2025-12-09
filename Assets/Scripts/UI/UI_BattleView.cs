using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 UI 视图，由 `BattleController` 管理。负责展示玩家与敌方的头像、血量/蓝量和两个交互按钮。
/// </summary>
public class UI_BattleView : MonoBehaviour
{
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
    private Image spiritImage;

    [SerializeField]
    private Image enemyImage;

    [SerializeField]
    private ImageBar spiritHpBar;

    [SerializeField]
    private ImageBar spiritMpBar;

    [SerializeField]
    private ImageBar enemyHpBar;

    [SerializeField]
    private ImageBar enemyMpBar;

    [Header("Debug / Info")]
    [SerializeField]
    private TMP_Text turnText;

    private BattleController controller;
    private BattleModel model;

    public void Bind(BattleController ctrl, BattleModel m)
    {
        Unbind();

        controller = ctrl;
        model = m;

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);

        if (skillButton1 != null)
            skillButton1.onClick.AddListener(() => OnSkillClicked(0));

        if (skillButton2 != null)
            skillButton2.onClick.AddListener(() => OnSkillClicked(1));

        if (skillButton3 != null)
            skillButton3.onClick.AddListener(() => OnSkillClicked(2));

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

        Debug.LogWarning($"UI: Skill {skillIndex} clicked but no BattleController bound or found in scene.");
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
        if (spiritImage != null && player != null)
        {
            if (player.Image != null)
                spiritImage.sprite = player.Image;
        }

        if (enemyImage != null && enemy != null)
        {
            if (enemy.Image != null)
                enemyImage.sprite = enemy.Image;
        }

        // 血量/蓝量：使用单位公开的属性，不直接依赖数据对象字段名
        if (spiritHpBar != null && player != null)
            spiritHpBar.Set(player.HP, player.MaxHP);

        if (spiritMpBar != null && player != null)
            spiritMpBar.Set(player.Mana, player.MaxMana);

        if (enemyHpBar != null && enemy != null)
            enemyHpBar.Set(enemy.HP, enemy.MaxHP);

        if (enemyMpBar != null && enemy != null)
            enemyMpBar.Set(enemy.Mana, enemy.MaxMana);

        if (turnText != null && model != null)
        {
            turnText.text = $"Turn: {model.CurrentTurn}";
        }

        // 更新技能按钮状态
        UpdateSkillButtons();
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
            UpdateButtonText(button, $"{skillName}\n{description}\n次数:0/{skill.MaxUsesPerBattle}");
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
            UpdateButtonText(button, $"{skillName}\n{description}\n蓝耗:{manaCost} | 次数:{remainingUses}/{skill.MaxUsesPerBattle}");
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
}
