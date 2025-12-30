using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreamWeavers.Rooms
{
public class RestRoom_cza : RoomBase_cza
{
    [Header("UI 按钮绑定")]
    [Tooltip("休息按钮（可选）。若未手动绑定，将在运行时按名称尝试自动查找并绑定。")]
    [SerializeField] private UnityEngine.UI.Button restButton;

    [Header("动画设置")]
    [Tooltip("休息动画的Animator组件（可选）")]
    [SerializeField] private Animator restAnimator;
    
    [Tooltip("播放动画时需要显示的对象（可选，如果动画对象默认隐藏）")]
    [SerializeField] private GameObject animationTarget;
    
    [Tooltip("休息动画的触发器名称")]
    [SerializeField] private string restAnimationTrigger = "Rest";
    
    [Tooltip("休息后进入路线选择的延迟时间（秒）")]
    [SerializeField] private float delayBeforeRouteSelection = 3f;

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

        var battleController = GameObject.FindObjectOfType<BattleController>(true);
        if (battleController == null)
        {
            Debug.LogWarning("[RestRoom] No BattleController found");
            return;
        }

        // 收集所有已部署的精灵
        var deployedSpirits = CollectDeployedSpiritsWithIndex(battleController);
        int healedCount = 0;

        foreach (var (spiritData, index) in deployedSpirits)
        {
            if (spiritData == null)
                continue;

            // 获取精灵的运行时数据
            var runtimeData = battleController.GetSpiritRuntimeData(index);

            // 跳过已死亡的精灵（HP = 0）
            if (runtimeData.CurrentHP <= 0)
            {
                Debug.Log($"[RestRoom] Skipping dead spirit: {spiritData.DisplayName}");
                continue;
            }

            // 计算治疗量（20% HP和MP）
            int healHP = Mathf.CeilToInt(runtimeData.MaxHP * 0.2f);
            int healMP = Mathf.CeilToInt(runtimeData.MaxMP * 0.2f);

            // 更新运行时数据
            int newHP = Mathf.Min(runtimeData.CurrentHP + healHP, runtimeData.MaxHP);
            int newMP = Mathf.Min(runtimeData.CurrentMP + healMP, runtimeData.MaxMP);

            battleController.UpdateSpiritRuntimeData(index, newHP, newMP);

            healedCount++;
            Debug.Log($"[RestRoom] Healed {spiritData.DisplayName}: HP {runtimeData.CurrentHP}->{newHP}, MP {runtimeData.CurrentMP}->{newMP}");
        }

        Debug.Log($"[RestRoom] Healed {healedCount}/{deployedSpirits.Count} spirit(s)");

        rested = true;

        // 禁用按钮
        if (restButton != null)
        {
            restButton.interactable = false;
        }

        // 播放休息动画
        if (restAnimator != null)
        {
            // 如果指定了动画目标对象，确保它是激活的
            if (animationTarget != null && !animationTarget.activeInHierarchy)
            {
                animationTarget.SetActive(true);
                Debug.Log($"[RestRoom] 激活动画目标对象: {animationTarget.name}");
            }
            
            restAnimator.SetTrigger(restAnimationTrigger);
            Debug.Log($"[RestRoom] 播放休息动画，触发器: {restAnimationTrigger}");
        }

        // 启动延迟协程，等待后进入路线选择
        StartCoroutine(DelayedRouteSelection());
    }

    /// <summary>
    /// 延迟进入路线选择的协程
    /// </summary>
    private IEnumerator DelayedRouteSelection()
    {
        Debug.Log($"[RestRoom] 等待 {delayBeforeRouteSelection} 秒后进入路线选择...");
        yield return new WaitForSeconds(delayBeforeRouteSelection);
        animationTarget.SetActive(false);

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

    /// <summary>
    /// 收集所有已部署的精灵及其索引
    /// </summary>
    private List<(SpiritData, int)> CollectDeployedSpiritsWithIndex(BattleController battleController)
    {
        var list = new List<(SpiritData, int)>();

        if (battleController == null)
        {
            Debug.LogWarning("[RestRoom] BattleController is null");
            return list;
        }

        var spiritQueue = battleController.GetSpiritQueue();
        if (spiritQueue != null)
        {
            for (int i = 0; i < spiritQueue.Count; i++)
            {
                list.Add((spiritQueue[i], i));
            }
        }

        Debug.Log($"[RestRoom] Collected {list.Count} deployed spirit(s)");
        return list;
    }
}
}