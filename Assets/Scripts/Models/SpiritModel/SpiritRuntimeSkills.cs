using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only skill overrides for SpiritData.
/// Keeps ScriptableObject assets immutable during play by storing temporary skills separately.
/// </summary>
public static class SpiritRuntimeSkills
{
    private static readonly Dictionary<SpiritData, HashSet<ScriptableObject>> runtimeAddedSkills = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetOnPlay()
    {
        ClearAll();
    }

    public static void ClearAll()
    {
        runtimeAddedSkills.Clear();
    }

    public static void ClearForSpirit(SpiritData spirit)
    {
        if (spirit == null)
            return;

        runtimeAddedSkills.Remove(spirit);
    }

    public static bool EnsureSkill(SpiritData spirit, ScriptableObject skill)
    {
        if (spirit == null || skill == null)
            return false;

        if (!runtimeAddedSkills.TryGetValue(spirit, out var set))
        {
            set = new HashSet<ScriptableObject>();
            runtimeAddedSkills[spirit] = set;
        }

        if (set.Contains(skill))
            return true;

        set.Add(skill);
        return true;
    }

    public static bool HasRuntimeSkill(SpiritData spirit, ScriptableObject skill)
    {
        if (spirit == null || skill == null)
            return false;

        return runtimeAddedSkills.TryGetValue(spirit, out var set) && set.Contains(skill);
    }

    public static IReadOnlyList<ScriptableObject> GetAllSkillObjects(SpiritData spirit)
    {
        var list = new List<ScriptableObject>();
        if (spirit == null)
            return list;

        var dedupe = new HashSet<ScriptableObject>();

        if (spirit.Skills != null)
        {
            for (int i = 0; i < spirit.Skills.Length; i++)
            {
                var skill = spirit.Skills[i];
                if (skill == null)
                    continue;
                if (dedupe.Add(skill))
                    list.Add(skill);
            }
        }

        if (runtimeAddedSkills.TryGetValue(spirit, out var set))
        {
            foreach (var skill in set)
            {
                if (skill == null)
                    continue;
                if (dedupe.Add(skill))
                    list.Add(skill);
            }
        }

        return list;
    }
}

