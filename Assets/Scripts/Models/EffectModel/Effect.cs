using UnityEngine;

/// <summary>
/// Base class for all gameplay effects that can be attached to a skill.
/// </summary>
public abstract class Effect : ScriptableObject
{
    [SerializeField]
    private string displayName;

    [SerializeField, TextArea]
    private string description;

    /// <summary>
    /// Display-friendly name exposed in the inspector and UI.
    /// Falls back to the asset name when no custom label is provided.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    /// <summary>
    /// Optional descriptive text for UI/tooltip usage.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// Executes the effect logic against the provided caster/target pair.
    /// </summary>
    public abstract void Apply(IBattleUnit caster, IBattleUnit target);
}
