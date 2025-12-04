using UnityEngine;

public enum BattleState
{
    None,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat,
}

public class BattleController : MonoBehaviour
{
    [SerializeField]
    private SpiritData playerData;

    [SerializeField]
    private EnemyData enemyData;

    private Spirit player;
    private Enemy enemy;
    private AIController enemyAI;

    [SerializeField]
    private UI_BattleView battleView;

    private BattleModel model;

    // 缓存的 HP/Mana 值，用于检测变化
    private int lastPlayerHP;
    private int lastPlayerMana;
    private int lastEnemyHP;
    private int lastEnemyMana;

    public BattleState State { get; private set; } = BattleState.None;

    private void Start()
    {
        // 场景加载时自动初始化战斗
        InitializeBattle();
        Debug.Log("BattleController: InitializeBattle called in Start()");
    }

    public void InitializeBattle()
    {
        player = new Spirit(playerData);
        enemy = new Enemy(enemyData);
        enemyAI = new AIController();

        // 创建并初始化战斗模型，由本 Controller 管理
        model = new BattleModel();
        model.InitializeBattle(player, enemy);

        // 绑定 UI（如果存在）
        if (battleView != null)
            battleView.Bind(this, model);

        // 初始化缓存值
        lastPlayerHP = player?.HP ?? 0;
        lastPlayerMana = player?.Mana ?? 0;
        lastEnemyHP = enemy?.HP ?? 0;
        lastEnemyMana = enemy?.Mana ?? 0;

        State = BattleState.PlayerTurn;
    }

    public Spirit Player => player;
    public Enemy Enemy => enemy;

    public void PlayerUseSkill(ISkill skill)
    {
        if (State != BattleState.PlayerTurn || skill == null || player == null || enemy == null)
        {
            Debug.Log(
                $"BattleController: PlayerUseSkill failed - State check failed or null units"
            );
            return;
        }

        if (player.Mana < skill.ManaCost)
        {
            Debug.Log(
                $"BattleController: Not enough mana. Required: {skill.ManaCost}, Current: {player.Mana}"
            );
            return;
        }

        Debug.Log(
            $"BattleController: PlayerUseSkill called. Skill={skill.DisplayName}, ManaCost={skill.ManaCost}"
        );

        // 扣除蓝量
        player.ConsumeMana(skill.ManaCost);
        Debug.Log($"BattleController: Mana consumed. Remaining: {player.Mana}");

        // 执行技能
        Debug.Log($"BattleController: Executing skill on enemy...");
        skill.Execute(player, enemy);
        Debug.Log($"BattleController: Enemy HP after skill: {enemy.HP}");

        // 更新模型中的羁绊/状态并刷新 UI
        model?.UpdateActiveSynergies();
        UpdateBattleStateAfterAction();
        if (battleView != null)
            battleView.Refresh();
    }

    /// <summary>
    /// 尝试使用玩家的第一个技能（由 UI 调用）。
    /// </summary>
    public void UseFirstPlayerSkill()
    {
        Debug.Log("BattleController: UseFirstPlayerSkill called");

        if (player == null)
        {
            Debug.Log("BattleController: Player is null!");
            return;
        }

        var skills = player.GetSkills();
        Debug.Log($"BattleController: Found {skills?.Count ?? 0} skills");

        if (skills != null && skills.Count > 0)
        {
            var skill = skills[0];
            Debug.Log($"BattleController: Using skill: {skill?.DisplayName ?? "null"}");
            PlayerUseSkill(skill);
        }
        else
        {
            Debug.Log("BattleController: No skills available!");
        }
    }

    public void EndPlayerTurn()
    {
        if (State != BattleState.PlayerTurn)
            return;

        Debug.Log("BattleController: EndPlayerTurn called.");

        // 增加回合计数（模型负责）
        model?.IncrementTurn();

        State = BattleState.EnemyTurn;
        EnemyAct();

        if (battleView != null)
            battleView.Refresh();
    }

    private void EnemyAct()
    {
        if (enemyAI == null || enemy == null || player == null)
            return;

        enemyAI.TakeTurn(enemy, player);
        model?.UpdateActiveSynergies();
        UpdateBattleStateAfterAction();

        if (State == BattleState.EnemyTurn)
            State = BattleState.PlayerTurn;

        if (battleView != null)
            battleView.Refresh();
    }

    private void UpdateBattleStateAfterAction()
    {
        if (enemy != null && enemy.IsDead)
        {
            State = BattleState.Victory;
            return;
        }

        if (player != null && player.IsDead)
        {
            State = BattleState.Defeat;
            return;
        }
    }

    private void Update()
    {
        // 监测玩家/敌方 HP 或 Mana 的变化；若变化则刷新 UI（不修改 Spirit/Enemy 源码）
        bool changed = false;
        if (player != null)
        {
            if (player.HP != lastPlayerHP || player.Mana != lastPlayerMana)
            {
                lastPlayerHP = player.HP;
                lastPlayerMana = player.Mana;
                changed = true;
            }
        }

        if (enemy != null)
        {
            if (enemy.HP != lastEnemyHP || enemy.Mana != lastEnemyMana)
            {
                lastEnemyHP = enemy.HP;
                lastEnemyMana = enemy.Mana;
                changed = true;
            }
        }

        if (changed && battleView != null)
            battleView.Refresh();
    }
}
