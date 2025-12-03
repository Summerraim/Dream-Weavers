using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime synergy data that tracks trigger counts and exposes the owner's base stats.
/// </summary>
public class SynergyModel
{
    public Synergy Synergy { get; }
    public Spirit Owner { get; }
    public IReadOnlyList<int> TriggerCounts => triggerCounts;
    public int ActiveCount { get; private set; }

    private readonly List<int> triggerCounts;

    public SynergyModel(Spirit owner, Synergy synergy)
    {
        Owner = owner;
        Synergy = synergy;
        triggerCounts = synergy != null ? new List<int>(synergy.TriggerCounts) : new List<int>();
    }

    /// <summary>
    /// Updates the active unit count and immediately lets the synergy reapply its effect.
    /// </summary>
    public void SetActiveCount(int count)
    {
        ActiveCount = Mathf.Max(0, count);
        Synergy?.Apply(this);
    }

    public int BaseMaxHP => Owner?.BaseMaxHP ?? 0;
    public int BaseDamage => Owner?.BaseDamage ?? 0;
    public int BaseDefense => Owner?.BaseDefense ?? 0;

    public int GetCurrentTierIndex()
    {
        if (triggerCounts == null || triggerCounts.Count == 0)
            return -1;

        int tier = -1;
        for (int i = 0; i < triggerCounts.Count; i++)
        {
            if (ActiveCount >= triggerCounts[i])
                tier = i;
        }
        return tier;
    }

    public int GetRequiredCountForTier(int tierIndex)
    {
        if (tierIndex < 0 || triggerCounts == null || tierIndex >= triggerCounts.Count)
            return -1;
        return triggerCounts[tierIndex];
    }
}
