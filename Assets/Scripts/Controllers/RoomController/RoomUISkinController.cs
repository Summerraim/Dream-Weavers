using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 放在各房间类型面板（如 Panel_Combat）下，用于按楼层应用不同美术。
public class RoomUISkinController : MonoBehaviour
{
    [Header("Skin 容器")]
    [Tooltip("承载楼层皮肤实例的挂点，不指定则使用当前对象。")]
    [SerializeField]
    private Transform skinRoot;

    [Header("楼层皮肤配置")]
    [Tooltip("按楼层指定要实例化的皮肤 Prefab（背景/特效/装饰等）。")]
    [SerializeField]
    private List<FloorSkinEntry> floorSkins = new List<FloorSkinEntry>();

    [Tooltip("找不到对应楼层时的默认皮肤（可选）。")]
    [SerializeField]
    private GameObject defaultSkinPrefab;

    [Header("实例化策略")]
    [Tooltip("若 skinRoot 下已存在与目标 Prefab 同名的子物体，则直接收编为当前皮肤，避免再次实例化。")]
    [SerializeField]
    private bool adoptExistingChild = true;

    [Tooltip("应用新皮肤后清理 skinRoot 下除当前皮肤外的其他子物体，避免重复背景残留。")]
    [SerializeField]
    private bool clearOthersOnApply = true;

    [Tooltip("切换楼层时隐藏上一层皮肤而不是销毁（便于返回或复用）。")]
    [SerializeField]
    private bool hidePreviousOnApply = true;

    private GameObject currentSkinInstance;
    private int currentAppliedFloor = int.MinValue;

    // 缓存皮肤预制体中的路线选择按钮
    private Button skinRouteBtn1;
    private Button skinRouteBtn2;
    private Button skinRouteBtn3;

    [Header("路线选择UI（按楼层管理）")]
    [Tooltip("选择阶段的路线界面根容器（可选，仅用于显示/隐藏，不负责实例化）。")]
    [SerializeField]
    private Transform chooseUIRoot;

    [Serializable]
    public struct FloorChooseUIEntry
    {
        public int floorIndex;
        [Tooltip("该楼层对应的路线选择UI根物体（已在场景/面板中存在）。")]
        public GameObject uiRoot;
    }

    [Tooltip("为每个楼层配置已存在的路线选择UI根物体，控制显示/隐藏以避免跨楼层残留。")]
    [SerializeField]
    private List<FloorChooseUIEntry> floorChooseUIs = new List<FloorChooseUIEntry>();

    [Serializable]
    public struct FloorSkinEntry
    {
        public int floorIndex;
        public GameObject skinPrefab;
    }

    private void Awake()
    {
        if (skinRoot == null)
            skinRoot = transform;
        if (chooseUIRoot == null)
            chooseUIRoot = transform;
        Debug.Log($"[SkinCtl] Awake skinRoot={(skinRoot!=null?skinRoot.name:"null")} chooseUIRoot={(chooseUIRoot!=null?chooseUIRoot.name:"null")} floorSkins.Count={floorSkins.Count} on {gameObject.name}");
    }

    // 调试用：返回配置的皮肤数量
    public int GetFloorSkinsCount() => floorSkins != null ? floorSkins.Count : 0;

    // 在进入房间时调用，按楼层切换皮肤。重复调用同一楼层会跳过。
    public void ApplySkin(int floorIndex)
    {
        Debug.Log($"[SkinCtl] ApplySkin called: floorIndex={floorIndex} currentAppliedFloor={currentAppliedFloor} currentSkinInstance={(currentSkinInstance!=null?currentSkinInstance.name:"null")} floorSkins.Count={floorSkins.Count} on {gameObject.name}");
        
        if (floorIndex == currentAppliedFloor && currentSkinInstance != null)
        {
            // 即使是同一楼层，也要确保皮肤是激活的（可能被其他逻辑隐藏过）
            if (!currentSkinInstance.activeSelf)
            {
                currentSkinInstance.SetActive(true);
                Debug.Log($"[SkinCtl] ApplySkin re-activate (same floor) floor={floorIndex} instance={currentSkinInstance.name} activeInHierarchy={currentSkinInstance.activeInHierarchy}");
            }
            else
            {
                Debug.Log($"[SkinCtl] ApplySkin skip (same floor) floor={floorIndex} instance={currentSkinInstance.name} activeSelf={currentSkinInstance.activeSelf} activeInHierarchy={currentSkinInstance.activeInHierarchy}");
            }
            return;
        }

        // 处理旧实例：根据配置选择隐藏或销毁
        if (currentSkinInstance != null)
        {
            if (hidePreviousOnApply)
            {
                currentSkinInstance.SetActive(false);
                Debug.Log($"[SkinCtl] Hide previous skin -> {currentSkinInstance.name}");
            }
            else
            {
                if (Application.isPlaying)
                    Destroy(currentSkinInstance);
                else
                    DestroyImmediate(currentSkinInstance);
                Debug.Log($"[SkinCtl] Destroy previous skin -> {currentSkinInstance.name}");
            }
            currentSkinInstance = null;
        }

        var prefab = ResolvePrefab(floorIndex);
        Debug.Log($"[SkinCtl] Resolve prefab floor={floorIndex} -> {(prefab!=null?prefab.name:"null")} (defaultSkinPrefab={(defaultSkinPrefab!=null?defaultSkinPrefab.name:"null")})");
        if (prefab != null)
        {
            // 可选：收编已存在的同名子物体，避免重复生成
            if (adoptExistingChild)
            {
                var existing = FindChildByName(skinRoot, prefab.name);
                if (existing != null)
                {
                    currentSkinInstance = existing.gameObject;
                    // 确保被收编的皮肤是激活状态（可能之前被隐藏过）
                    currentSkinInstance.SetActive(true);
                    Debug.Log($"[SkinCtl] Adopt existing child -> {existing.name} (activated)");
                }
                else
                {
                    Debug.Log($"[SkinCtl] No existing child named {prefab.name}, will instantiate");
                }
            }

            // 若未找到现有同名子物体，则实例化
            if (currentSkinInstance == null)
            {
                currentSkinInstance = Instantiate(prefab, skinRoot);
                currentSkinInstance.name = prefab.name;
                // 确保新实例化的皮肤是激活状态
                currentSkinInstance.SetActive(true);
                Debug.Log($"[SkinCtl] Instantiate skin -> {currentSkinInstance.name} under {skinRoot.name}");
            }

            // 自动绑定皮肤预制体中的路线选择按钮
            BindRouteButtonsInSkin(currentSkinInstance);

            // 可选：清理除当前皮肤外的其他子物体，保证唯一（支持隐藏或销毁）
            if (clearOthersOnApply)
            {
                if (hidePreviousOnApply)
                {
                    DeactivateOtherChildren(skinRoot, currentSkinInstance.transform);
                    Debug.Log($"[SkinCtl] Deactivate other children under {skinRoot.name}");
                }
                else
                {
                    CleanupOtherChildren(skinRoot, currentSkinInstance.transform);
                    Debug.Log($"[SkinCtl] Cleanup other children under {skinRoot.name}");
                }
            }
        }
        currentAppliedFloor = floorIndex;
        Debug.Log($"[SkinCtl] Applied floor={floorIndex} currentSkin={(currentSkinInstance!=null?currentSkinInstance.name:"null")} activeSelf={(currentSkinInstance!=null?currentSkinInstance.activeSelf.ToString():"N/A")} activeInHierarchy={(currentSkinInstance!=null?currentSkinInstance.activeInHierarchy.ToString():"N/A")} skinRoot.activeInHierarchy={skinRoot.gameObject.activeInHierarchy}");
    }

    // 在进入选择阶段时调用：只显示当前楼层对应的路线选择UI，隐藏其他楼层的选择UI。
    // active=true 显示当前层UI，active=false 隐藏所有层UI。
    public void ApplyChooseUI(int floorIndex, bool active)
    {
        if (floorChooseUIs == null || floorChooseUIs.Count == 0)
            return;
        Debug.Log($"[SkinCtl] ApplyChooseUI floor={floorIndex} active={active} entries={floorChooseUIs.Count}");
        for (int i = 0; i < floorChooseUIs.Count; i++)
        {
            var entry = floorChooseUIs[i];
            var go = entry.uiRoot;
            if (go == null) continue;
            
            // 跳过当前皮肤实例，避免皮肤被错误隐藏
            if (currentSkinInstance != null && go == currentSkinInstance)
            {
                Debug.Log($"[SkinCtl] ChooseUI entry floor={entry.floorIndex} -> Skip (is current skin) {go.name}");
                continue;
            }
            
            bool show = active && entry.floorIndex == floorIndex;
            go.SetActive(show);
            Debug.Log($"[SkinCtl] ChooseUI entry floor={entry.floorIndex} -> {(show?"Show":"Hide")} {go.name}");
        }
    }

    private GameObject ResolvePrefab(int floorIndex)
    {
        // 调试：打印所有配置条目
        Debug.Log($"[SkinCtl] ResolvePrefab: checking {floorSkins.Count} entries for floor={floorIndex}");
        for (int i = 0; i < floorSkins.Count; i++)
        {
            var entry = floorSkins[i];
            Debug.Log($"[SkinCtl]   Entry[{i}]: floorIndex={entry.floorIndex} skinPrefab={(entry.skinPrefab != null ? entry.skinPrefab.name : "NULL")}");
        }
        
        // 精确匹配
        for (int i = 0; i < floorSkins.Count; i++)
        {
            if (floorSkins[i].floorIndex == floorIndex && floorSkins[i].skinPrefab != null)
                return floorSkins[i].skinPrefab;
        }

        // 可选：寻找不超过当前楼层的最近皮肤（向下兼容），例如 1→1, 2→2, 3→2
        int best = int.MinValue;
        GameObject bestPrefab = null;
        for (int i = 0; i < floorSkins.Count; i++)
        {
            var e = floorSkins[i];
            if (e.skinPrefab == null)
                continue;
            if (e.floorIndex <= floorIndex && e.floorIndex > best)
            {
                best = e.floorIndex;
                bestPrefab = e.skinPrefab;
            }
        }
        if (bestPrefab != null)
            return bestPrefab;

        // 向上兼容：寻找大于当前楼层的最近皮肤（例如 0→1 或 1→2）
        int upperBest = int.MaxValue;
        GameObject upperPrefab = null;
        for (int i = 0; i < floorSkins.Count; i++)
        {
            var e = floorSkins[i];
            if (e.skinPrefab == null)
                continue;
            if (e.floorIndex > floorIndex && e.floorIndex < upperBest)
            {
                upperBest = e.floorIndex;
                upperPrefab = e.skinPrefab;
            }
        }
        if (upperPrefab != null)
            return upperPrefab;

        // 兜底：列表里第一个有效皮肤
        for (int i = 0; i < floorSkins.Count; i++)
        {
            if (floorSkins[i].skinPrefab != null)
                return floorSkins[i].skinPrefab;
        }

        // 兜底默认
        return defaultSkinPrefab;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (string.Equals(c.name, childName, StringComparison.Ordinal))
                return c;
        }
        return null;
    }

    private void CleanupOtherChildren(Transform root, Transform keep)
    {
        if (root == null) return;
        var toDestroy = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (keep != null && c == keep) continue;
            toDestroy.Add(c);
        }
        for (int i = 0; i < toDestroy.Count; i++)
        {
            var go = toDestroy[i].gameObject;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }

    private void DeactivateOtherChildren(Transform root, Transform keep)
    {
        if (root == null) return;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (keep != null && c == keep) continue;
            c.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 从皮肤预制体中查找并绑定路线选择按钮（按名称包含 Next1/Next2/Next3 或 Choice1/Choice2/Choice3 等）
    /// </summary>
    private void BindRouteButtonsInSkin(GameObject skinInstance)
    {
        if (skinInstance == null) return;

        // 清空旧引用
        skinRouteBtn1 = null;
        skinRouteBtn2 = null;
        skinRouteBtn3 = null;

        var buttons = skinInstance.GetComponentsInChildren<Button>(true);

        foreach (var btn in buttons)
        {
            var btnName = btn.gameObject.name;
            if (skinRouteBtn1 == null && ContainsAnyKeyword(btnName, "Next1", "Next01", "Next_1", "Choice1", "Choice01", "Choice_1", "Route1", "Route01", "Route_1"))
            {
                skinRouteBtn1 = btn;
            }
            else if (skinRouteBtn2 == null && ContainsAnyKeyword(btnName, "Next2", "Next02", "Next_2", "Choice2", "Choice02", "Choice_2", "Route2", "Route02", "Route_2"))
            {
                skinRouteBtn2 = btn;
            }
            else if (skinRouteBtn3 == null && ContainsAnyKeyword(btnName, "Next3", "Next03", "Next_3", "Choice3", "Choice03", "Choice_3", "Route3", "Route03", "Route_3"))
            {
                skinRouteBtn3 = btn;
            }
        }

        // 绑定事件（移除旧监听，添加新监听）
        if (skinRouteBtn1 != null)
        {
            skinRouteBtn1.onClick.RemoveAllListeners();
            skinRouteBtn1.onClick.AddListener(() =>
            {
                Debug.Log("[SkinCtl] Click Route Button 1");
                RoomStateMachine_cza.Instance?.GoToNext(0);
            });
            Debug.Log($"[SkinCtl] 绑定皮肤按钮: {skinRouteBtn1.gameObject.name} -> GoToNext(0)");
        }
        if (skinRouteBtn2 != null)
        {
            skinRouteBtn2.onClick.RemoveAllListeners();
            skinRouteBtn2.onClick.AddListener(() =>
            {
                Debug.Log("[SkinCtl] Click Route Button 2");
                RoomStateMachine_cza.Instance?.GoToNext(1);
            });
            Debug.Log($"[SkinCtl] 绑定皮肤按钮: {skinRouteBtn2.gameObject.name} -> GoToNext(1)");
        }
        if (skinRouteBtn3 != null)
        {
            skinRouteBtn3.onClick.RemoveAllListeners();
            skinRouteBtn3.onClick.AddListener(() =>
            {
                Debug.Log("[SkinCtl] Click Route Button 3");
                RoomStateMachine_cza.Instance?.GoToNext(2);
            });
            Debug.Log($"[SkinCtl] 绑定皮肤按钮: {skinRouteBtn3.gameObject.name} -> GoToNext(2)");
        }

        if (skinRouteBtn1 == null && skinRouteBtn2 == null && skinRouteBtn3 == null)
        {
            Debug.Log($"[SkinCtl] 皮肤 {skinInstance.name} 中未找到路线选择按钮（这可能是正常的，如果该皮肤不包含路线选择UI）");
        }
    }

    /// <summary>
    /// 更新皮肤中路线选择按钮的标签和交互状态
    /// </summary>
    public void UpdateRouteButtons(IReadOnlyList<int> choices, bool selecting)
    {
        int count = choices != null ? choices.Count : 0;
        
        // 更新按钮1
        if (skinRouteBtn1 != null)
        {
            skinRouteBtn1.interactable = selecting && count >= 1;
            UpdateButtonLabel(skinRouteBtn1, choices, 0);
        }
        
        // 更新按钮2
        if (skinRouteBtn2 != null)
        {
            skinRouteBtn2.interactable = selecting && count >= 2;
            UpdateButtonLabel(skinRouteBtn2, choices, 1);
        }
        
        // 更新按钮3
        if (skinRouteBtn3 != null)
        {
            skinRouteBtn3.interactable = selecting && count >= 3;
            UpdateButtonLabel(skinRouteBtn3, choices, 2);
        }
        
        Debug.Log($"[SkinCtl] UpdateRouteButtons: selecting={selecting}, count={count}, btn1={skinRouteBtn1 != null}, btn2={skinRouteBtn2 != null}, btn3={skinRouteBtn3 != null}");
    }

    private void UpdateButtonLabel(Button button, IReadOnlyList<int> choices, int index)
    {
        if (button == null) return;
        
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        
        if (choices != null && index < choices.Count)
        {
            int roomId = choices[index];
            var sm = RoomStateMachine_cza.Instance;
            if (sm != null && sm.CurrentMap != null && sm.CurrentMap.Rooms.TryGetValue(roomId, out var node))
            {
                label.text = $"路线{index + 1}: {node.Type}";
            }
            else
            {
                label.text = $"路线{index + 1}: 房间{roomId}";
            }
        }
        else
        {
            label.text = $"路线{index + 1}: --";
        }
    }

    /// <summary>
    /// 检查是否有绑定的路线按钮
    /// </summary>
    public bool HasRouteButtons()
    {
        return skinRouteBtn1 != null || skinRouteBtn2 != null || skinRouteBtn3 != null;
    }

    private bool ContainsAnyKeyword(string source, params string[] keywords)
    {
        if (string.IsNullOrEmpty(source)) return false;
        foreach (var k in keywords)
        {
            if (!string.IsNullOrEmpty(k) && source.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }
}
