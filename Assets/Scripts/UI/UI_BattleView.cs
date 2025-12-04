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

    [SerializeField]
    private Button skillButton;

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

        if (skillButton != null)
            skillButton.onClick.AddListener(OnSkillClicked);

        Refresh();
    }

    public void Unbind()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);

        if (skillButton != null)
            skillButton.onClick.RemoveListener(OnSkillClicked);

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

    private void OnSkillClicked()
    {
        Debug.Log("UI: Skill button clicked.");
        if (controller != null)
        {
            controller.UseFirstPlayerSkill();
            return;
        }

        var fallback = FindObjectOfType<BattleController>();
        if (fallback != null)
        {
            Debug.Log(
                "UI: controller is null, fallback to scene BattleController for UseFirstPlayerSkill"
            );
            fallback.UseFirstPlayerSkill();
            return;
        }

        Debug.LogWarning("UI: Skill clicked but no BattleController bound or found in scene.");
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
        // 头像：优先从数据对象中查找可能存在的 Sprite 字段（如 Portrait/Icon/Sprite），否则保持在 Inspector 中手动设置的图片
        if (spiritImage != null && player != null)
        {
            if (TryGetSpriteFromData(player.Data, out var s))
                spiritImage.sprite = s;
        }

        if (enemyImage != null && enemy != null)
        {
            if (TryGetSpriteFromData(enemy.Data, out var s))
                enemyImage.sprite = s;
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
    }

    private bool TryGetSpriteFromData(object dataObj, out Sprite sprite)
    {
        sprite = null;
        if (dataObj == null)
            return false;

        var t = dataObj.GetType();

        // 尝试字段
        var fieldNames = new[] { "Portrait", "Icon", "Sprite" };
        foreach (var name in fieldNames)
        {
            var f = t.GetField(name);
            if (f != null)
            {
                var val = f.GetValue(dataObj) as Sprite;
                if (val != null)
                {
                    sprite = val;
                    return true;
                }
            }
        }

        // 尝试属性
        foreach (var name in fieldNames)
        {
            var p = t.GetProperty(name);
            if (p != null)
            {
                var val = p.GetValue(dataObj, null) as Sprite;
                if (val != null)
                {
                    sprite = val;
                    return true;
                }
            }
        }

        return false;
    }
}
