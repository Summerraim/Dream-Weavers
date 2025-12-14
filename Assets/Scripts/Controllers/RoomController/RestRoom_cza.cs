using System.Collections.Generic;
using UnityEngine;

namespace DreamWeavers.Rooms
{
public class RestRoom_cza : RoomBase_cza
{
    private void Awake()
    {
        Type = RoomType_cza.Rest;
    }

    public override void EnterRoom()
    {
        // 在休息房：为所有可访问的 Spirit 恢复20%生命与20%法力
        var targets = CollectSpirits();
        foreach (var spirit in targets)
        {
            if (spirit == null) continue;
            int healHP = Mathf.CeilToInt(spirit.MaxHP * 0.2f);
            int healMana = Mathf.CeilToInt(spirit.MaxMana * 0.2f);
            spirit.ReceiveHeal(healHP);
            spirit.ReceiveMana(healMana);
        }
        Debug.Log($"RestRoom: Applied rest to {targets.Count} spirit(s).");
    }

    public override void ExitRoom()
    {
        // 休息房离开时无需额外处理，可按需扩展
    }

    private List<Spirit> CollectSpirits()
    {
        var list = new List<Spirit>();

        // 尝试从战斗控制器中获取当前玩家 Spirit
        var controllers = GameObject.FindObjectsOfType<BattleController>();
        foreach (var bc in controllers)
        {
            if (bc != null && bc.Player != null)
            {
                list.Add(bc.Player);
            }
        }

        // TODO: 如有队伍/编队管理器，可在此补充收集逻辑
        return list;
    }
}
}