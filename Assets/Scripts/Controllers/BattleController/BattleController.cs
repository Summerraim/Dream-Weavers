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

    public BattleState State { get; private set; } = BattleState.None;

    public void InitializeBattle()
    {
        player = new Spirit(playerData);
        enemy = new Enemy(enemyData);
        enemyAI = new AIController();

        State = BattleState.PlayerTurn;
    }

    public Spirit Player => player;
    public Enemy Enemy => enemy;

    public void PlayerUseSkill(ISkill skill)
    {
        if (State != BattleState.PlayerTurn || skill == null || player == null || enemy == null)
            return;

        if (player.Mana < skill.ManaCost)
            return;

        skill.Execute(player, enemy);
        UpdateBattleStateAfterAction();
    }

    public void EndPlayerTurn()
    {
        if (State != BattleState.PlayerTurn)
            return;

        State = BattleState.EnemyTurn;
        EnemyAct();
    }

    private void EnemyAct()
    {
        if (enemyAI == null || enemy == null || player == null)
            return;

        enemyAI.TakeTurn(enemy, player);
        UpdateBattleStateAfterAction();

        if (State == BattleState.EnemyTurn)
            State = BattleState.PlayerTurn;
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
}
