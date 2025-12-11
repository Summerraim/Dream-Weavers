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

    private GameObject currentSkinInstance;
    private int currentAppliedFloor = int.MinValue;

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
    }

    // 在进入房间时调用，按楼层切换皮肤。重复调用同一楼层会跳过。
    public void ApplySkin(int floorIndex)
    {
        if (floorIndex == currentAppliedFloor && currentSkinInstance != null)
            return;

        // 清理旧实例
        if (currentSkinInstance != null)
        {
            if (Application.isPlaying)
                Destroy(currentSkinInstance);
            else
                DestroyImmediate(currentSkinInstance);
            currentSkinInstance = null;
        }

        var prefab = ResolvePrefab(floorIndex);
        if (prefab != null)
        {
            // 可选：收编已存在的同名子物体，避免重复生成
            if (adoptExistingChild)
            {
                var existing = FindChildByName(skinRoot, prefab.name);
                if (existing != null)
                {
                    currentSkinInstance = existing.gameObject;
                }
            }

            // 若未找到现有同名子物体，则实例化
            if (currentSkinInstance == null)
            {
                currentSkinInstance = Instantiate(prefab, skinRoot);
                currentSkinInstance.name = prefab.name;
            }

            // 可选：清理除当前皮肤外的其他子物体，保证唯一
            if (clearOthersOnApply)
            {
                CleanupOtherChildren(skinRoot, currentSkinInstance.transform);
            }
        }
        currentAppliedFloor = floorIndex;
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
}
