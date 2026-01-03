#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DreamWeaversRuntimeDataTools
{
    [MenuItem("Tools/Dream Weavers/Runtime/Clear Runtime Skill Overrides")]
    private static void ClearRuntimeSkillOverrides()
    {
        SpiritRuntimeSkills.ClearAll();
        Debug.Log("[DreamWeavers] Cleared SpiritRuntimeSkills (runtime-only skill overrides).");
    }

    [MenuItem("Tools/Dream Weavers/Runtime/Reset Player (Initial) + Clear Runtime", true)]
    private static bool ValidateResetPlayerAndClearRuntime()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem("Tools/Dream Weavers/Runtime/Reset Player (Initial) + Clear Runtime")]
    private static void ResetPlayerAndClearRuntime()
    {
        SpiritRuntimeSkills.ClearAll();
        PlayerManager.Instance.ResetToInitialState();
        Debug.Log("[DreamWeavers] Reset player to initial state and cleared runtime overrides.");
    }
}
#endif

