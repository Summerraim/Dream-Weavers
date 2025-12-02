using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 羁绊基类，类似 Effect，持有基础描述与触发档位。
/// </summary>
public abstract class Synergy : ScriptableObject
{
    [SerializeField]
    private string synergyId;

    [SerializeField]
    private string displayName;

    [SerializeField, TextArea]
    private string description;

    [SerializeField]
    private List<int> triggerCounts = new List<int> { 2, 4, 6 };

    public string SynergyId => string.IsNullOrWhiteSpace(synergyId) ? name : synergyId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<int> TriggerCounts => triggerCounts;

    /// <summary>
    /// 当触发数量发生变化时调用，内部自行检查档位并对 Spirit 施加效果。
    /// </summary>
    public abstract void Apply(SynergyModel model);
}
