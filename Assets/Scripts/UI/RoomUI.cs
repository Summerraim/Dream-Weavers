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
    private Coroutine enemyWatchCo; // 监听战斗房间敌人状态的协程
    // private bool overrideCompleteByEnemy; // 已废弃
    // private bool combatCompleteReady; // 已废弃

    [Header("调试开关")]
    [Tooltip("测试模式：强制 Complete 按钮始终可见且可点击，忽略战斗房间的敌人状态")] 
    [SerializeField]
    private bool forceCompleteAlwaysActive = true;

    [Header("面板切换")]
    [Tooltip("房间类型到UI面板名的映射，用于进入房间时切换UI。")]
    [SerializeField]
    private List<TypePanelMapping> typePanelMappings = new List<TypePanelMapping>();

    [Tooltip("分支选择时显示的面板名（例如包含三个Next按钮的面板）")]
    [SerializeField]
    private string choosePanelName = "Panel_ChooseNext";
    private Dictionary<RoomType_cza, string> typePanelMap;

    // 当前已显示的房间类型面板名，用于在进入选择阶段时立刻隐藏
    private string currentRoomPanelName;

    [Header("路线选择Prefab（按楼层）")]
    [Tooltip("用于显示分支选择的Prefab容器（实例化到该节点下）。为空则使用本对象")] 
    [SerializeField]
    private Transform chooseRoot;

    [Tooltip("仅在选择阶段使用Prefab，不显示默认选择面板（choosePanelName）")]
    [SerializeField]
    private bool usePrefabOnlyForChoices = true;

    [Serializable]
    public struct FloorChoosePrefabEntry
    {
        public int floorIndex;
        public GameObject prefab;
    }

    [Tooltip("不同楼层使用不同的路线选择Prefab；未匹配则不显示Prefab（仅使用面板逻辑）")]
    [SerializeField]
    private List<FloorChoosePrefabEntry> floorChoosePrefabs = new List<FloorChoosePrefabEntry>();

    private GameObject currentChooseInstance;

    [Header("技能房 UI")]
    [Tooltip("显示技能信息的文本（技能名字、法力消耗、描述）")]
    [SerializeField]
    private TextMeshProUGUI skillInfoText;
    
    [Tooltip("显示技能将添加给哪个精灵的文本")]
    [SerializeField]
    private TextMeshProUGUI spiritInfoText;
    [Serializable]
    public struct TypePanelMapping
    {
        public RoomType_cza type;
        public string panelName;
    }

    private void Awake()
    {
        // 强校验：关键引用不能为空
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
        bool refsOk = btnNext1 != null && btnNext2 != null && btnNext3 != null;
        if (!refsOk)
        {
            if (btnNext1)
                btnNext1.interactable = false;
            if (btnNext2)
                btnNext2.interactable = false;
            if (btnNext3)
                btnNext3.interactable = false;
        }

        // 构建类型映射字典
        BuildTypePanelMap();

        if (chooseRoot == null)
            chooseRoot = transform;
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
            RoomStateMachine_cza.Instance.OnFloorInitialized -= OnFloorInitializedHidePanels;
            subscribed = false;
        }
        if (waitCo != null)
        {
            StopCoroutine(waitCo);
            waitCo = null;
        }
        // 停止战斗监听
        if (enemyWatchCo != null)
        {
            StopCoroutine(enemyWatchCo);
            enemyWatchCo = null;
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

        // 根据房间类型更新 Complete 按钮文案：战斗房间显示“捕捉”，其他显示“离开房间”
        UpdateCompleteButtonLabel(node.Type);

        // 进入房间后尝试监听战斗房间敌人状态
        RestartWatchEnemyIfCombatRoom();
        // 技能房：更新技能信息 UI
        if (node.Type == RoomType_cza.Skill)
        {
            UpdateSkillRoomUI();
        }
        else
        {
            ClearSkillRoomUI();
        }    }

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
            if (!usePrefabOnlyForChoices)
                ShowPanel(choosePanelName, true);
            // 显示按楼层路选择Prefab
            ShowChoosePrefab(true);
            // 同步调用房间类型面板上的皮肤控制器，显示当前楼层的路线选择 UI（场景内已存在的 uiRoot）
            var sm = RoomStateMachine_cza.Instance;
            int floor = (sm != null && sm.CurrentMap != null) ? sm.CurrentMap.FloorIndex : 0;
            TryApplyChooseUIOnSkinController(floor, true);
            // 进入选择阶段停止战斗监听
            if (enemyWatchCo != null)
            {
                StopCoroutine(enemyWatchCo);
                enemyWatchCo = null;
            }
        }
        else
        {
            if (!usePrefabOnlyForChoices)
                ShowPanel(choosePanelName, false);
            ShowChoosePrefab(false);
            // 退出选择阶段隐藏所有路线选择 UI
            var sm = RoomStateMachine_cza.Instance;
            int floor = (sm != null && sm.CurrentMap != null) ? sm.CurrentMap.FloorIndex : 0;
            TryApplyChooseUIOnSkinController(floor, false);
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
        // 仅保留Next按钮交互由RefreshChoiceUI设置
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
        RoomStateMachine_cza.Instance.OnFloorInitialized += OnFloorInitializedHidePanels;
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
        if (!usePrefabOnlyForChoices)
            ShowPanel(choosePanelName, false);
        ShowChoosePrefab(false);
        // 完成后停止战斗监听
        if (enemyWatchCo != null)
        {
            StopCoroutine(enemyWatchCo);
            enemyWatchCo = null;
        }
    }

    // 新增：楼层初始化完成时，统一隐藏上一层的类型面板以避免皮肤残留
    private void OnFloorInitializedHidePanels(int floorIndex)
    {
        HideAllRegisteredPanels();
        currentRoomPanelName = null;
        // 楼层变化时选择面板也关闭，等待进入新房间后再由刷新逻辑打开
        if (!usePrefabOnlyForChoices)
            ShowPanel(choosePanelName, false);
        // 同时停止战斗监听，重置交互覆盖状态
        if (enemyWatchCo != null)
        {
            StopCoroutine(enemyWatchCo);
            enemyWatchCo = null;
        }
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
        if (UIManagerService.Instance == null)
            return;
        if (typePanelMap == null || typePanelMap.Count == 0)
            BuildTypePanelMap();

        // 先隐藏所有已注册的房间类型面板和分支选择面板，确保只显示当前房间UI
        HideAllRegisteredPanels();

        if (typePanelMap.TryGetValue(type, out var panelName) && !string.IsNullOrEmpty(panelName)) //通过传入的房间类型找到对应面板
        {
            ShowPanel(panelName, true);
            currentRoomPanelName = panelName;

            // 进入房间后按楼层应用美术皮肤（复用交互UI，仅替换背景/装饰）
            var panelGO = UIManagerService.Instance.GetPanel(panelName);
            Debug.Log($"[RoomUI] SwitchToRoomTypePanel: panelName={panelName} panelGO={(panelGO!=null ? panelGO.name : "null")} type={type}");
            if (panelGO != null)
            {
                var skinCtl = panelGO.GetComponentInChildren<RoomUISkinController>(true);
                int floor = 0;
                var sm = RoomStateMachine_cza.Instance;
                if (sm != null && sm.CurrentMap != null)
                    floor = sm.CurrentMap.FloorIndex;
                Debug.Log($"[RoomUI] SkinCtl found={(skinCtl!=null)} floor={floor}");
                if (skinCtl != null)
                    skinCtl.ApplySkin(floor);
            }
        }
    }

    // 尝试在房间类型面板的皮肤控制器上应用选择 UI 显隐
    private void TryApplyChooseUIOnSkinController(int floor, bool active)
    {
        if (string.IsNullOrEmpty(currentRoomPanelName) || UIManagerService.Instance == null)
            return;
        var panelGO = UIManagerService.Instance.GetPanel(currentRoomPanelName);
        var skinCtl = panelGO != null ? panelGO.GetComponentInChildren<RoomUISkinController>(true) : null;
        if (skinCtl != null)
        {
            skinCtl.ApplyChooseUI(floor, active);
            Debug.Log($"[RoomUI] ApplyChooseUI via SkinCtl: floor={floor} active={active} panel={currentRoomPanelName}");
        }
        else
        {
            Debug.Log($"[RoomUI] SkinCtl not found under panel {currentRoomPanelName}, skip ApplyChooseUI");
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
        if (UIManagerService.Instance == null || string.IsNullOrEmpty(panelName))
            return;
        if (active)
            UIManagerService.Instance.ShowPanel(panelName);
        else
            UIManagerService.Instance.HidePanel(panelName);
    }

    private GameObject ResolveChoosePrefabForCurrentFloor()
    {
        if (floorChoosePrefabs == null || floorChoosePrefabs.Count == 0)
            return null;
        int floor = 0;
        var sm = RoomStateMachine_cza.Instance;
        if (sm != null && sm.CurrentMap != null)
            floor = sm.CurrentMap.FloorIndex;
        for (int i = 0; i < floorChoosePrefabs.Count; i++)
        {
            var e = floorChoosePrefabs[i];
            if (e.floorIndex == floor && e.prefab != null)
                return e.prefab;
        }
        return null;
    }

    private void ShowChoosePrefab(bool active)
    {
        var targetPrefab = ResolveChoosePrefabForCurrentFloor();
        if (!active)
        {
            if (currentChooseInstance != null)
            {
                currentChooseInstance.SetActive(false);
            }
            return;
        }
        if (targetPrefab == null)
        {
            // 未配置对应楼层的Prefab，保持现有面板逻辑
            if (currentChooseInstance != null)
                currentChooseInstance.SetActive(false);
            return;
        }
        // 若当前实例与目标Prefab不同或不存在，则实例化
        if (currentChooseInstance == null || currentChooseInstance.name != targetPrefab.name)
        {
            // 隐藏旧实例
            if (currentChooseInstance != null)
                currentChooseInstance.SetActive(false);
            var parent = chooseRoot != null ? chooseRoot : transform;
            currentChooseInstance = Instantiate(targetPrefab, parent);
            currentChooseInstance.name = targetPrefab.name;
        }
        currentChooseInstance.SetActive(true);
    }

    // 根据当前房间类型更新 Complete 按钮的显示文本
    private void UpdateCompleteButtonLabel(RoomType_cza type)
    {
        // 已废弃
    }

    // 战斗房间监听：若存在 CombatRoom_cza，则监控其敌人模型的 HP/Mana
    private void RestartWatchEnemyIfCombatRoom()
    {
        var room = FindObjectOfType<DreamWeavers.Rooms.CombatRoom_cza>();
        if (room == null)
        {
            // 无战斗房间，关闭覆盖
            if (enemyWatchCo != null)
            {
                StopCoroutine(enemyWatchCo);
                enemyWatchCo = null;
            }
            return;
        }
        var combatRoom = FindObjectOfType<DreamWeavers.Rooms.CombatRoom_cza>();
        if (combatRoom == null)
        {
            if (enemyWatchCo != null)
            {
                StopCoroutine(enemyWatchCo);
                enemyWatchCo = null;
            }
            return;
        }
        if (enemyWatchCo != null)
        {
            StopCoroutine(enemyWatchCo);
        }
        enemyWatchCo = StartCoroutine(WatchEnemyState(combatRoom));
    }

    private IEnumerator WatchEnemyState(DreamWeavers.Rooms.CombatRoom_cza room)
    {
        // 等待敌人模型就绪
        while (room != null && room.GetEnemyModel() == null)
        {
            yield return null;
        }
        var model = room != null ? room.GetEnemyModel() : null;
        if (model == null)
        {
            yield break;
        }
        // 轮询检查生命/法力
        while (room != null && model != null)
        {
            if (model.HP <= 0 || model.Mana <= 0)
            {
                // 直接完成房间并进入路线选择
                if (RoomStateMachine_cza.Instance != null)
                {
                    RoomStateMachine_cza.Instance.CompleteCurrentRoom();
                    ShowPanel(choosePanelName, true);
                }
                yield break;
            }
            yield return null;
        }
    }

    #region 技能房 UI

    /// <summary>
    /// 更新技能房 UI 显示
    /// </summary>
    private void UpdateSkillRoomUI()
    {
        var skillRoom = FindObjectOfType<DreamWeavers.Rooms.SkillRoom_cza>();
        if (skillRoom == null)
        {
            Debug.LogWarning("[RoomUI] 未找到 SkillRoom_cza 实例");
            ClearSkillRoomUI();
            return;
        }

        // 获取技能信息
        var selectedSkill = skillRoom.GetSelectedSkill();
        var matchedSpirit = skillRoom.GetMatchedSpirit();
        bool granted = skillRoom.IsSkillGranted();

        // 更新技能信息文本
        if (skillInfoText != null)
        {
            if (selectedSkill != null)
            {
                string skillName = selectedSkill.DisplayName;
                int manaCost = selectedSkill.ManaCost;
                string description = selectedSkill.Description ?? "无描述";
                
                skillInfoText.text = $"<b>{skillName}</b>\n" +
                                     $"<color=#4A90D9>法力消耗: {manaCost}</color>\n" +
                                     $"{description}";
            }
            else
            {
                skillInfoText.text = "未获取到技能";
            }
        }

        // 更新精灵信息文本
        if (spiritInfoText != null)
        {
            if (matchedSpirit != null)
            {
                string spiritName = string.IsNullOrWhiteSpace(matchedSpirit.DisplayName) 
                    ? matchedSpirit.name 
                    : matchedSpirit.DisplayName;
                
                if (granted)
                {
                    spiritInfoText.text = $"<color=#00FF00>已成功添加给: <b>{spiritName}</b></color>";
                }
                else
                {
                    spiritInfoText.text = $"将添加给: <b><color=#FFD700>{spiritName}</color></b>";
                }
            }
            else
            {
                spiritInfoText.text = "无匹配精灵";
            }
        }

        Debug.Log($"[RoomUI] UpdateSkillRoomUI: skill={(selectedSkill != null ? selectedSkill.DisplayName : "null")}, spirit={(matchedSpirit != null ? matchedSpirit.DisplayName : "null")}, granted={granted}");
    }

    /// <summary>
    /// 清空技能房 UI
    /// </summary>
    private void ClearSkillRoomUI()
    {
        if (skillInfoText != null)
        {
            skillInfoText.text = "";
        }
        if (spiritInfoText != null)
        {
            spiritInfoText.text = "";
        }
    }

    #endregion
}
