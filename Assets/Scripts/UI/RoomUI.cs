using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIActions_cza : MonoBehaviour
{
    [Header("显示")]
    [SerializeField]
    private TextMeshProUGUI currentRoomText;

    [SerializeField]
    private TextMeshProUGUI nextRoomsText;

    [Header("按钮")]
    [SerializeField]
    private Button btnComplete;

    [SerializeField]
    private Button btnNext1;

    [SerializeField]
    private Button btnNext2;

    [SerializeField]
    private Button btnNext3;

    // [SerializeField] private Button btnRandom;
    // [SerializeField] private Button btnReInit; // 可选

    // [SerializeField] private int reinitFloor = 1;

    private bool subscribed;
    private Coroutine waitCo;

    [Header("面板切换")]
    [Tooltip("房间类型到UI面板名的映射，用于进入房间时切换UI。")]
    [SerializeField] private List<TypePanelMapping> typePanelMappings = new List<TypePanelMapping>();
    [Tooltip("分支选择时显示的面板名（例如包含三个Next按钮的面板）")]
    [SerializeField] private string choosePanelName = "Panel_ChooseNext";
    private Dictionary<RoomType_cza, string> typePanelMap;
    // 当前已显示的房间类型面板名，用于在进入选择阶段时立刻隐藏
    private string currentRoomPanelName;

    [Serializable]
    public struct TypePanelMapping
    {
        public RoomType_cza type;
        public string panelName;
    }

    private void Awake()
    {
        // 强校验：关键引用不能为空
        if (btnComplete == null)
            Debug.LogError("[RoomUI] btnComplete 未赋值，请在 Inspector 绑定 Complete 按钮");
        if (btnNext1 == null)
            Debug.LogError("[RoomUI] btnNext1 未赋值，请在 Inspector 绑定 Next1 按钮");
        if (btnNext2 == null)
            Debug.LogError("[RoomUI] btnNext2 未赋值，请在 Inspector 绑定 Next2 按钮");
        if (btnNext3 == null)
            Debug.LogError("[RoomUI] btnNext3 未赋值，请在 Inspector 绑定 Next3 按钮");
        if (currentRoomText == null)
            Debug.LogError("[RoomUI] currentRoomText 未赋值，请在 Inspector 绑定当前房间文本");
        if (nextRoomsText == null)
            Debug.LogError("[RoomUI] nextRoomsText 未赋值，请在 Inspector 绑定分支/可选文本");

        // 自动绑定缺失引用（按名称包含匹配）
        AutoBindReferences();

        // 绑定按钮
        if (btnComplete)
            btnComplete.onClick.AddListener(() =>
            {
                Debug.Log("[RoomUI] Click Complete");
                if (RoomStateMachine_cza.Instance != null)
                {
                    // 触发当前房间完成，状态机会生成分支并标记选择阶段
                    RoomStateMachine_cza.Instance.CompleteCurrentRoom();
                    // 立即显式显示选择面板，确保 UI 及时可见
                    ShowPanel(choosePanelName, true);
                }
                else
                {
                    Debug.LogWarning("[RoomUI] RoomStateMachine Instance 为空，未挂载或未初始化");
                }
            });
        if (btnNext1)
            btnNext1.onClick.AddListener(() =>
            {
                Debug.Log("[RoomUI] Click Next1");
                RoomStateMachine_cza.Instance?.GoToNext(0);
            });
        if (btnNext2)
            btnNext2.onClick.AddListener(() =>
            {
                Debug.Log("[RoomUI] Click Next2");
                RoomStateMachine_cza.Instance?.GoToNext(1);
            });
        if (btnNext3)
            btnNext3.onClick.AddListener(() =>
            {
                Debug.Log("[RoomUI] Click Next3");
                RoomStateMachine_cza.Instance?.GoToNext(2);
            });
        // if (btnRandom) btnRandom.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToRandomNext());
        // if (btnReInit) btnReInit.onClick.AddListener(() => RoomStateMachine_cza.Instance.InitFloor(reinitFloor));

        // 避免文本拦截点击
        if (currentRoomText)
            currentRoomText.raycastTarget = false;
        if (nextRoomsText)
            nextRoomsText.raycastTarget = false;

        // 若关键引用缺失，主动禁用所有按钮，避免误操作
        bool refsOk =
            btnComplete != null && btnNext1 != null && btnNext2 != null && btnNext3 != null;
        if (!refsOk)
        {
            if (btnComplete)
                btnComplete.interactable = false;
            if (btnNext1)
                btnNext1.interactable = false;
            if (btnNext2)
                btnNext2.interactable = false;
            if (btnNext3)
                btnNext3.interactable = false;
        }

        // 构建类型映射字典
        BuildTypePanelMap();
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
            if (waitCo == null)
                waitCo = StartCoroutine(WaitAndSubscribe());
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

    private T FindInChildrenByName<T>(string[] keywords)
        where T : Component
    {
        var comps = GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            var name = c.gameObject.name;
            foreach (var k in keywords)
            {
                if (
                    !string.IsNullOrEmpty(k)
                    && name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0
                )
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
            RoomStateMachine_cza.Instance.OnRoomCompleted -= OnRoomCompletedHideCurrent;
            subscribed = false;
        }
        if (waitCo != null)
        {
            StopCoroutine(waitCo);
            waitCo = null;
        }
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

        // 进入房间时进行UI面板切换
        SwitchToRoomTypePanel(node.Type);
    }

    private string FormatList(System.Collections.Generic.List<int> list)
    {
        return string.Join(", ", list);
    }

    private void RefreshChoiceUI(System.Collections.Generic.IReadOnlyList<int> choices)
    {
        bool selecting =
            RoomStateMachine_cza.Instance != null && RoomStateMachine_cza.Instance.IsAwaitingChoice;
        int count = choices != null ? choices.Count : 0;
        if (btnNext1)
            btnNext1.interactable = selecting && count >= 1;
        if (btnNext2)
            btnNext2.interactable = selecting && count >= 2;
        if (btnNext3)
            btnNext3.interactable = selecting && count >= 3;

        // 统一应用 Complete 的交互状态并输出日志
        ApplyInteractableState($"RefreshChoiceUI(selecting={selecting}, choices={count})");
        if (nextRoomsText)
        {
            if (selecting)
                nextRoomsText.text = count > 0 ? $"可选: {string.Join(", ", choices)}" : "可选: 无";
            // 非选择阶段保留 RefreshRoomInfo 的静态提示
        }

        // 选择阶段显示选择面板；非选择阶段隐藏选择面板
        if (selecting)
        {
            // 立即隐藏当前房间的类型面板，实现进入选择阶段就遮蔽上一房间UI
            if (!string.IsNullOrEmpty(currentRoomPanelName))
            {
                ShowPanel(currentRoomPanelName, false);
            }
            ShowPanel(choosePanelName, true);
        }
        else
        {
            ShowPanel(choosePanelName, false);
            
        }
    }

    private void OnStateReady()
    {
        // 状态机初始化完成后触发：刷新文案和交互
        if (RoomStateMachine_cza.Instance != null)
        {
            var r = RoomStateMachine_cza.Instance.CurrentRoom;
            if (r != null)
                RefreshRoomInfo(r);
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
            // 始终保持 Complete 可点击并暴露在外，由点击行为触发选择面板显隐
            btnComplete.interactable = true;
            Debug.Log(
                $"[RoomUI] ApplyInteractableState[{reason}]: ready={ready} selecting={selecting} -> Complete.interactable=true"
            );
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
        if (subscribed || RoomStateMachine_cza.Instance == null)
            return;
        RoomStateMachine_cza.Instance.OnRoomEntered += RefreshRoomInfo;
        RoomStateMachine_cza.Instance.OnBranchChoicesUpdated += RefreshChoiceUI;
        RoomStateMachine_cza.Instance.OnReady += OnStateReady;
        RoomStateMachine_cza.Instance.OnRoomCompleted += OnRoomCompletedHideCurrent;
        subscribed = true;
        RefreshChoiceUI(RoomStateMachine_cza.Instance.GetCurrentBranchChoices());
        if (RoomStateMachine_cza.Instance.IsReady)
            OnStateReady();
    }

    // 房间完成时（包括Boss房触发楼层切换前），立即隐藏当前房间UI，避免跨楼层残留
    private void OnRoomCompletedHideCurrent(RoomNode_cza _)
    {
        // 防御性处理：隐藏所有已注册的类型面板，避免跨楼层残留
        HideAllRegisteredPanels();
        currentRoomPanelName = null;
        // 同时确保选择面板关闭
        ShowPanel(choosePanelName, false);
    }

    private void BuildTypePanelMap()
    {
        typePanelMap = new Dictionary<RoomType_cza, string>();
        foreach (var m in typePanelMappings)
        {
            if (!string.IsNullOrEmpty(m.panelName))
                typePanelMap[m.type] = m.panelName;
        }
    }

    private void SwitchToRoomTypePanel(RoomType_cza type)
    {
        if (UIManagerService.Instance == null) return;
        if (typePanelMap == null || typePanelMap.Count == 0) BuildTypePanelMap();
        if (typePanelMap.TryGetValue(type, out var panelName) && !string.IsNullOrEmpty(panelName))//通过传入的房间类型找到对应面板
        {
            // 简单策略：隐藏所有已注册面板，再显示目标面板；选择面板按需叠加
            HideAllRegisteredPanels();
            ShowPanel(panelName, true);
            currentRoomPanelName = panelName;

            // 进入房间后按楼层应用美术皮肤（复用交互UI，仅替换背景/装饰）
            var panelGO = UIManagerService.Instance.GetPanel(panelName);
            if (panelGO != null)
            {
                var skinCtl = panelGO.GetComponentInChildren<RoomUISkinController>(true);
                int floor = 0;
                var sm = RoomStateMachine_cza.Instance;
                if (sm != null && sm.CurrentMap != null) floor = sm.CurrentMap.FloorIndex;
                if (skinCtl != null) skinCtl.ApplySkin(floor);
            }
        }
    }

    private void HideAllRegisteredPanels()
    {
        var ui = UIManagerService.Instance;
        if (ui == null) return;

        if (typePanelMap == null || typePanelMap.Count == 0)
            BuildTypePanelMap();

        var namesToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in typePanelMap)
        {
            if (!string.IsNullOrEmpty(kv.Value))
                namesToHide.Add(kv.Value);
        }
        if (!string.IsNullOrEmpty(choosePanelName))
            namesToHide.Add(choosePanelName);

        foreach (var name in namesToHide)
        {
            var go = ui.GetPanel(name);
            if (go != null)
            {
                ui.HidePanel(name);
            }
        }
    }

    private void ShowPanel(string panelName, bool active)
    {
        if (UIManagerService.Instance == null || string.IsNullOrEmpty(panelName)) return;
        if (active) UIManagerService.Instance.ShowPanel(panelName);
        else UIManagerService.Instance.HidePanel(panelName);
    }
}
