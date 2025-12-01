using System;
using UnityEngine;

[Serializable]
public class PlayerData_cza
{
    // —— 玩家可携带的精灵数量上限 ——
    [SerializeField]
    private int carryLimit = 6;
    public int CarryLimit => carryLimit;

    // —— 玩家一次战斗可上场的精灵数量上限 ——
    [SerializeField]
    private int battleLimit = 1;
    public int BattleLimit => battleLimit;

    // —— 当属性变化时，发事件给UI 或 其他系统 ——
    public event Action OnLimitsChanged;

    public PlayerData_cza(int initialCarry, int initialBattle)
    {
        carryLimit = Mathf.Max(1, initialCarry);
        battleLimit = Mathf.Max(1, initialBattle);
    }

    // —— 修改携带上限 ——
    public void SetCarryLimit(int newLimit)
    {
        carryLimit = Mathf.Max(1, newLimit);
        OnLimitsChanged?.Invoke();
    }

    // —— 修改上场上限 ——
    public void SetBattleLimit(int newLimit)
    {
        battleLimit = Mathf.Max(1, newLimit);
        OnLimitsChanged?.Invoke();
    }
}