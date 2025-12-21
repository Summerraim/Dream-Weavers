using System.Collections.Generic;
using UnityEngine;

namespace DreamWeavers.Rooms
{
public class RestRoom_cza : RoomBase_cza
{
    [Header("UI 按钮绑定")]
    [Tooltip("休息按钮（可选）。若未手动绑定，将在运行时按名称尝试自动查找并绑定。")]
    [SerializeField] private UnityEngine.UI.Button restButton;

    private bool rested; // 是否已休息

    private void Awake()
    {
        Type = RoomType_cza.Rest;
        
        // 自动绑定休息按钮（名称包含 Rest）
        if (restButton == null)
        {
            var btns = GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in btns)
            {
                var n = b.gameObject.name;
                if (!string.IsNullOrEmpty(n) && n.IndexOf("Rest", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    restButton = b;
                    break;
                }
            }
        }
        
        if (restButton != null)
        {
            restButton.onClick.RemoveListener(OnClickRest);
            restButton.onClick.AddListener(OnClickRest);
            Debug.Log($"[RestRoom] Bound Rest button -> {restButton.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[RestRoom] Rest button not found in children (name contains 'Rest'). You can bind it via Inspector.");
        }
    }

    public override void EnterRoom()
    {
        rested = false;
        if (restButton != null)
        {
            restButton.interactable = true;
        }
        Debug.Log("[RestRoom] EnterRoom");
    }

    public override void ExitRoom()
    {
        // 休息房离开时无需额外处理，可按需扩展
    }

    /// <summary>
    /// UI按钮回调：点击"休息"时调用，恢复精灵状态并进入路线选择
    /// </summary>
    public void OnClickRest()
    {
        if (rested)
        {
            Debug.Log("[RestRoom] 已经休息过，无法重复休息");
            return;
        }
        
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
        Debug.Log($"[RestRoom] Applied rest to {targets.Count} spirit(s).");
        
        rested = true;
        
        // 禁用按钮
        if (restButton != null)
        {
            restButton.interactable = false;
        }
        
        // 触发路线选择
        if (RoomStateMachine_cza.Instance != null)
        {
            Debug.Log("[RestRoom] 休息完成，触发路线选择");
            RoomStateMachine_cza.Instance.CompleteCurrentRoom();
        }
        else
        {
            Debug.LogWarning("[RestRoom] RoomStateMachine_cza.Instance 为 null，无法触发路线选择");
        }
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