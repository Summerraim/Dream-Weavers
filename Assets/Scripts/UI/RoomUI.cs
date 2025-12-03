using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIActions_cza : MonoBehaviour
{
    [Header("显示")]
    [SerializeField] private TextMeshProUGUI currentRoomText;
    [SerializeField] private TextMeshProUGUI nextRoomsText;

    [Header("按钮")]
    [SerializeField] private Button btnComplete;
    [SerializeField] private Button btnNext1;
    [SerializeField] private Button btnNext2;
    [SerializeField] private Button btnNext3;
    // [SerializeField] private Button btnRandom;
    // [SerializeField] private Button btnReInit; // 可选

    // [SerializeField] private int reinitFloor = 1;

    private bool subscribed;
    private Coroutine waitCo;

    private void Awake()
    {
        // 强校验：关键引用不能为空
        if (btnComplete == null) Debug.LogError("[RoomUI] btnComplete 未赋值，请在 Inspector 绑定 Complete 按钮");
        if (btnNext1 == null) Debug.LogError("[RoomUI] btnNext1 未赋值，请在 Inspector 绑定 Next1 按钮");
        if (btnNext2 == null) Debug.LogError("[RoomUI] btnNext2 未赋值，请在 Inspector 绑定 Next2 按钮");
        if (btnNext3 == null) Debug.LogError("[RoomUI] btnNext3 未赋值，请在 Inspector 绑定 Next3 按钮");
        if (currentRoomText == null) Debug.LogError("[RoomUI] currentRoomText 未赋值，请在 Inspector 绑定当前房间文本");
        if (nextRoomsText == null) Debug.LogError("[RoomUI] nextRoomsText 未赋值，请在 Inspector 绑定分支/可选文本");

        // 自动绑定缺失引用（按名称包含匹配）
        AutoBindReferences();

        // 绑定按钮
        if (btnComplete) btnComplete.onClick.AddListener(() => {
            Debug.Log("[RoomUI] Click Complete");
            if (RoomStateMachine_cza.Instance != null) RoomStateMachine_cza.Instance.CompleteCurrentRoom();
            else Debug.LogWarning("[RoomUI] RoomStateMachine Instance 为空，未挂载或未初始化");
        });
        if (btnNext1) btnNext1.onClick.AddListener(() => {
            Debug.Log("[RoomUI] Click Next1");
            RoomStateMachine_cza.Instance?.GoToNext(0);
        });
        if (btnNext2) btnNext2.onClick.AddListener(() => {
            Debug.Log("[RoomUI] Click Next2");
            RoomStateMachine_cza.Instance?.GoToNext(1);
        });
        if (btnNext3) btnNext3.onClick.AddListener(() => {
            Debug.Log("[RoomUI] Click Next3");
            RoomStateMachine_cza.Instance?.GoToNext(2);
        });
        // if (btnRandom) btnRandom.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToRandomNext());
        // if (btnReInit) btnReInit.onClick.AddListener(() => RoomStateMachine_cza.Instance.InitFloor(reinitFloor));

        // 避免文本拦截点击
        if (currentRoomText) currentRoomText.raycastTarget = false;
        if (nextRoomsText) nextRoomsText.raycastTarget = false;

        // 若关键引用缺失，主动禁用所有按钮，避免误操作
        bool refsOk = btnComplete != null && btnNext1 != null && btnNext2 != null && btnNext3 != null;
        if (!refsOk)
        {
            if (btnComplete) btnComplete.interactable = false;
            if (btnNext1) btnNext1.interactable = false;
            if (btnNext2) btnNext2.interactable = false;
            if (btnNext3) btnNext3.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (RoomStateMachine_cza.Instance != null)
        {
            TrySubscribe();
        }
        else
        {
            // 状态机稍后创建时再订阅
            if (waitCo == null) waitCo = StartCoroutine(WaitAndSubscribe());
        }
        // 初始化交互状态统一走一次应用方法
        ApplyInteractableState("OnEnable");
    }

    private void AutoBindReferences()
    {
        // 文本
        if (currentRoomText == null)
        {
            currentRoomText = FindInChildrenByName<TextMeshProUGUI>(new[] { "Current", "Room" });
        }
        if (nextRoomsText == null)
        {
            nextRoomsText = FindInChildrenByName<TextMeshProUGUI>(new[] { "Next", "Branches" });
        }

        // 按钮
        if (btnComplete == null)
        {
            btnComplete = FindInChildrenByName<Button>(new[] { "Complete", "Button_Complete" });
        }
        if (btnNext1 == null)
        {
            btnNext1 = FindInChildrenByName<Button>(new[] { "Next1", "Next01", "Next_1" });
        }
        if (btnNext2 == null)
        {
            btnNext2 = FindInChildrenByName<Button>(new[] { "Next2", "Next02", "Next_2" });
        }
        if (btnNext3 == null)
        {
            btnNext3 = FindInChildrenByName<Button>(new[] { "Next3", "Next03", "Next_3" });
        }
    }

    private T FindInChildrenByName<T>(string[] keywords) where T : Component
    {
        var comps = GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            var name = c.gameObject.name;
            foreach (var k in keywords)
            {
                if (!string.IsNullOrEmpty(k) && name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return c;
                }
            }
        }
        return null;
    }

    private void OnDisable()
    {
        if (subscribed && RoomStateMachine_cza.Instance != null)
        {
            RoomStateMachine_cza.Instance.OnRoomEntered -= RefreshRoomInfo;
            RoomStateMachine_cza.Instance.OnBranchChoicesUpdated -= RefreshChoiceUI;
            RoomStateMachine_cza.Instance.OnReady -= OnStateReady;
            subscribed = false;
        }
        if (waitCo != null) { StopCoroutine(waitCo); waitCo = null; }
    }

    private void RefreshRoomInfo(RoomNode_cza node)
    {
        if (currentRoomText)
            currentRoomText.text = $"Room {node.Id} / Type: {node.Type}";

        if (nextRoomsText)
        {
            if (node.NextRooms == null || node.NextRooms.Count == 0)
                nextRoomsText.text = "Next: None (Boss or End)";
            else
                nextRoomsText.text = $"Branches: {FormatList(node.NextRooms)}";
        }

        // 进入房间后统一应用交互状态
        ApplyInteractableState("OnRoomEntered");
    }

    private string FormatList(System.Collections.Generic.List<int> list)
    {
        return string.Join(", ", list);
    }

    private void RefreshChoiceUI(System.Collections.Generic.IReadOnlyList<int> choices)
    {
        bool selecting = RoomStateMachine_cza.Instance != null && RoomStateMachine_cza.Instance.IsAwaitingChoice;
        int count = choices != null ? choices.Count : 0;
        if (btnNext1) btnNext1.interactable = selecting && count >= 1;
        if (btnNext2) btnNext2.interactable = selecting && count >= 2;
        if (btnNext3) btnNext3.interactable = selecting && count >= 3;

        // 统一应用 Complete 的交互状态并输出日志
        ApplyInteractableState($"RefreshChoiceUI(selecting={selecting}, choices={count})");
        if (nextRoomsText)
        {
            if (selecting)
                nextRoomsText.text = count > 0 ? $"可选: {string.Join(", ", choices)}" : "可选: 无";
            // 非选择阶段保留 RefreshRoomInfo 的静态提示
        }
    }

    private void OnStateReady()
    {
        // 状态机初始化完成后触发：刷新文案和交互
        if (RoomStateMachine_cza.Instance != null)
        {
            var r = RoomStateMachine_cza.Instance.CurrentRoom;
            if (r != null) RefreshRoomInfo(r);
            RefreshChoiceUI(RoomStateMachine_cza.Instance.GetCurrentBranchChoices());
            ApplyInteractableState("OnReady");
        }
    }

    // 统一设置按钮交互状态的唯一入口，避免多处分支互相覆盖
    private void ApplyInteractableState(string reason)
    {
        var sm = RoomStateMachine_cza.Instance;
        bool ready = sm != null && sm.CurrentRoom != null;
        bool selecting = sm != null && sm.IsAwaitingChoice;
        if (btnComplete)
        {
            // 业务规则：有当前房间且不在选择阶段 -> 可点击 Complete
            btnComplete.interactable = ready && !selecting;
            Debug.Log($"[RoomUI] ApplyInteractableState[{reason}]: ready={ready} selecting={selecting} -> Complete.interactable={btnComplete.interactable}");
        }
        // Next 按钮的交互由 RefreshChoiceUI 设置，这里不重复处理
    }

    private IEnumerator WaitAndSubscribe()
    {
        yield return new WaitUntil(() => RoomStateMachine_cza.Instance != null);
        waitCo = null;
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed || RoomStateMachine_cza.Instance == null) return;
        RoomStateMachine_cza.Instance.OnRoomEntered += RefreshRoomInfo;
        RoomStateMachine_cza.Instance.OnBranchChoicesUpdated += RefreshChoiceUI;
        RoomStateMachine_cza.Instance.OnReady += OnStateReady;
        subscribed = true;
        RefreshChoiceUI(RoomStateMachine_cza.Instance.GetCurrentBranchChoices());
        if (RoomStateMachine_cza.Instance.IsReady) OnStateReady();
    }
}
