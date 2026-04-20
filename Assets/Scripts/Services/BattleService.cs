using DreamWeavers.Rooms;
using UnityEngine;

public class BattleService
{
    public void SkipBattleAndContinue(
        CombatRoom_cza combatRoom,
        BossRoom_cza bossRoom,
        UI_BattleView battleView,
        System.Action endBattleAndDeactivate
    )
    {
        MarkRoomCleared(combatRoom, bossRoom);

        if (battleView != null)
        {
            battleView.HideBattlePanel();
        }

        endBattleAndDeactivate?.Invoke();
        CompleteCurrentRoom();
    }

    public void ContinueAfterVictory(
        CombatRoom_cza combatRoom,
        UI_BattleView battleView,
        System.Action endBattleAndDeactivate
    )
    {
        if (battleView != null)
        {
            battleView.HideEnemyDeathPanel();
            battleView.HideCapturePanel();
            battleView.HideBattlePanel();
        }

        CleanupCombatDropVisual(combatRoom);
        endBattleAndDeactivate?.Invoke();
        CompleteCurrentRoom();
    }

    public void CleanupCombatDropVisual(CombatRoom_cza combatRoom)
    {
        if (combatRoom != null)
        {
            combatRoom.CleanupDropVisual();
        }
    }

    public void HandleVictory(
        Enemy enemy,
        EnemyData enemyData,
        CombatRoom_cza combatRoom,
        BossRoom_cza bossRoom,
        UI_BattleView battleView
    )
    {
        if (combatRoom != null)
        {
            HandleCombatRoomVictory(enemyData, combatRoom, battleView);
        }
        else if (bossRoom != null)
        {
            HandleBossRoomVictory(enemy, enemyData, bossRoom, battleView);
        }
        else
        {
            Debug.LogWarning("BattleService: combatRoom and bossRoom references are both null");
        }

        if (enemyData != null)
        {
            EnemyPool.MarkEnemyAsDefeated(enemyData);
        }
    }

    public bool TryHandleEarlyCapture(
        Spirit player,
        Enemy enemy,
        BattleModel model,
        CombatRoom_cza combatRoom,
        BossRoom_cza bossRoom,
        UI_BattleView battleView
    )
    {
        if (player == null || enemy == null || enemy.IsDead || model == null)
        {
            return false;
        }

        var buffs = model.GetBuffsForUnit(player);
        foreach (var buff in buffs)
        {
            if (buff is not HunterMasterBuff hunterBuff)
            {
                continue;
            }

            if (!hunterBuff.CanCaptureEnemy(enemy))
            {
                break;
            }

            Debug.Log("BattleService: HunterMaster trap activated! Enemy can be captured early.");

            if (battleView != null)
            {
                battleView.ShowEnemyDeathPanel();
            }

            bool success = false;
            SpiritData capturedSpirit = null;

            if (combatRoom != null)
            {
                (success, capturedSpirit) = combatRoom.AttemptCapture(earlyCapture: true);
            }
            else if (bossRoom != null)
            {
                (success, capturedSpirit) = bossRoom.AttemptCapture(earlyCapture: true);
            }

            ShowCaptureResult(battleView, success, capturedSpirit);
            return true;
        }

        return false;
    }

    private void HandleCombatRoomVictory(
        EnemyData enemyData,
        CombatRoom_cza combatRoom,
        UI_BattleView battleView
    )
    {
        combatRoom.MarkAsCleared();

        if (battleView != null)
        {
            battleView.ShowEnemyDeathPanel();
        }

        var (success, capturedSpirit) = combatRoom.AttemptCapture();
        ShowCaptureResult(battleView, success, capturedSpirit);
        combatRoom.RemoveCurrentEnemyFromPool();

        Debug.Log(
            $"BattleService: Combat victory settled for enemy {(enemyData != null ? enemyData.name : "null")}"
        );
    }

    private void HandleBossRoomVictory(
        Enemy enemy,
        EnemyData enemyData,
        BossRoom_cza bossRoom,
        UI_BattleView battleView
    )
    {
        bossRoom.MarkAsCleared();

        string bossName = enemy?.DisplayName ?? string.Empty;
        bool usedSpecialCG = battleView != null
            && battleView.ShowSpecialBossCG(
                bossName,
                () => HandleBossCaptureAfterCG(bossRoom, battleView)
            );

        if (!usedSpecialCG)
        {
            if (battleView != null)
            {
                battleView.ShowEnemyDeathPanel();
            }

            HandleBossCaptureAfterCG(bossRoom, battleView);
        }

        Debug.Log(
            $"BattleService: Boss victory settled for enemy {(enemyData != null ? enemyData.name : "null")}"
        );
    }

    private void HandleBossCaptureAfterCG(BossRoom_cza bossRoom, UI_BattleView battleView)
    {
        if (bossRoom == null)
        {
            Debug.LogWarning("BattleService: HandleBossCaptureAfterCG bossRoom is null");
            return;
        }

        var (success, capturedSpirit) = bossRoom.AttemptCapture();
        ShowCaptureResult(battleView, success, capturedSpirit);

        if (battleView != null)
        {
            battleView.ShowEnemyDeathPanel();
        }
    }

    private void MarkRoomCleared(CombatRoom_cza combatRoom, BossRoom_cza bossRoom)
    {
        if (combatRoom != null)
        {
            combatRoom.MarkAsCleared();
            return;
        }

        if (bossRoom != null)
        {
            bossRoom.MarkAsCleared();
        }
    }

    private void CompleteCurrentRoom()
    {
        if (RoomStateMachine_cza.Instance != null)
        {
            RoomStateMachine_cza.Instance.CompleteCurrentRoom();
        }
        else
        {
            Debug.LogWarning("BattleService: RoomStateMachine_cza.Instance is null");
        }
    }

    private void ShowCaptureResult(UI_BattleView battleView, bool success, SpiritData capturedSpirit)
    {
        if (battleView == null)
        {
            return;
        }

        if (success && capturedSpirit != null)
        {
            battleView.ShowCaptureSuccess(capturedSpirit.DisplayName);
        }
        else
        {
            battleView.ShowCaptureFailed();
        }
    }
}
