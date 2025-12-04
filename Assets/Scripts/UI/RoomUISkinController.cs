using System;
using System.Collections.Generic;
using UnityEngine;

// 放在各房间类型面板（如 Panel_Combat）下，用于按楼层应用不同美术。
public class RoomUISkinController : MonoBehaviour
{
    [Header("Skin 容器")]
    [Tooltip("承载楼层皮肤实例的挂点，不指定则使用当前对象。")]
    [SerializeField] private Transform skinRoot;

    [Header("楼层皮肤配置")]
    [Tooltip("按楼层指定要实例化的皮肤 Prefab（背景/特效/装饰等）。")]
    [SerializeField] private List<FloorSkinEntry> floorSkins = new List<FloorSkinEntry>();

    [Tooltip("找不到对应楼层时的默认皮肤（可选）。")]
    [SerializeField] private GameObject defaultSkinPrefab;

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
        if (skinRoot == null) skinRoot = transform;
    }

    // 在进入房间时调用，按楼层切换皮肤。重复调用同一楼层会跳过。
    public void ApplySkin(int floorIndex)
    {
        if (floorIndex == currentAppliedFloor && currentSkinInstance != null) return;

        // 清理旧实例
        if (currentSkinInstance != null)
        {
            if (Application.isPlaying) Destroy(currentSkinInstance);
            else DestroyImmediate(currentSkinInstance);
            currentSkinInstance = null;
        }

        var prefab = ResolvePrefab(floorIndex);
        if (prefab != null)
        {
            currentSkinInstance = Instantiate(prefab, skinRoot);
            currentSkinInstance.name = prefab.name;
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
        int best = int.MinValue; GameObject bestPrefab = null;
        for (int i = 0; i < floorSkins.Count; i++)
        {
            var e = floorSkins[i];
            if (e.skinPrefab == null) continue;
            if (e.floorIndex <= floorIndex && e.floorIndex > best)
            {
                best = e.floorIndex; bestPrefab = e.skinPrefab;
            }
        }
        if (bestPrefab != null) return bestPrefab;

        // 兜底默认
        return defaultSkinPrefab;
    }
}
