using System;
using System.Collections.Generic;
using UnityEngine;

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
        Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Awake skinRoot={(skinRoot!=null?skinRoot.name:"null")} chooseUIRoot={(chooseUIRoot!=null?chooseUIRoot.name:"null")} floorSkins.Count={floorSkins.Count}");
    }

    // 在进入房间时调用，按楼层切换皮肤。重复调用同一楼层会跳过。
    public void ApplySkin(int floorIndex)
    {
        if (floorIndex == currentAppliedFloor && currentSkinInstance != null)
        {
            Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] ApplySkin skip (same floor) floor={floorIndex} instance={currentSkinInstance.name}");
            return;
        }

        // 处理旧实例：根据配置选择隐藏或销毁
        if (currentSkinInstance != null)
        {
            if (hidePreviousOnApply)
            {
                currentSkinInstance.SetActive(false);
                Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Hide previous skin -> {currentSkinInstance.name}");
            }
            else
            {
                if (Application.isPlaying)
                    Destroy(currentSkinInstance);
                else
                    DestroyImmediate(currentSkinInstance);
                Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Destroy previous skin -> {currentSkinInstance.name}");
            }
            currentSkinInstance = null;
        }

        var prefab = ResolvePrefab(floorIndex);
        Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Resolve prefab floor={floorIndex} -> {(prefab!=null?prefab.name:"null")}");
        if (prefab != null)
        {
            // 可选：收编已存在的同名子物体，避免重复生成
            if (adoptExistingChild)
            {
                var existing = FindChildByName(skinRoot, prefab.name);
                if (existing != null)
                {
                    currentSkinInstance = existing.gameObject;
                    Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Adopt existing child -> {existing.name}");
                }
                else
                {
                    Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] No existing child named {prefab.name}, will instantiate");
                }
            }

            // 若未找到现有同名子物体，则实例化
            if (currentSkinInstance == null)
            {
                currentSkinInstance = Instantiate(prefab, skinRoot);
                currentSkinInstance.name = prefab.name;
                Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Instantiate skin -> {currentSkinInstance.name} under {skinRoot.name}");
            }

            // 可选：清理除当前皮肤外的其他子物体，保证唯一（支持隐藏或销毁）
            if (clearOthersOnApply)
            {
                if (hidePreviousOnApply)
                {
                    DeactivateOtherChildren(skinRoot, currentSkinInstance.transform);
                    Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Deactivate other children under {skinRoot.name}");
                }
                else
                {
                    CleanupOtherChildren(skinRoot, currentSkinInstance.transform);
                    Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Cleanup other children under {skinRoot.name}");
                }
            }
        }
        currentAppliedFloor = floorIndex;
        Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] Applied floor={floorIndex} currentSkin={(currentSkinInstance!=null?currentSkinInstance.name:"null")}");
    }

    // 在进入选择阶段时调用：只显示当前楼层对应的路线选择UI，隐藏其他楼层的选择UI。
    // active=true 显示当前层UI，active=false 隐藏所有层UI。
    public void ApplyChooseUI(int floorIndex, bool active)
    {
        if (floorChooseUIs == null || floorChooseUIs.Count == 0)
            return;
        Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] ApplyChooseUI floor={floorIndex} active={active} entries={floorChooseUIs.Count}");
        for (int i = 0; i < floorChooseUIs.Count; i++)
        {
            var entry = floorChooseUIs[i];
            var go = entry.uiRoot;
            if (go == null) continue;
            bool show = active && entry.floorIndex == floorIndex;
            go.SetActive(show);
            Debug.unityLogger.Log("RoomUISkin", $"[SkinCtl] ChooseUI entry floor={entry.floorIndex} -> {(show?"Show":"Hide")} {go.name}");
        }
    }

    private GameObject ResolvePrefab(int floorIndex)
    {
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
}
