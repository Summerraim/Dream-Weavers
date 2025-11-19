using UnityEngine;

public interface ISkill
{
    string DisplayName { get; }
    int ManaCost { get; }
    int CooldownTurns { get; }
    void Execute(Spirit caster, Spirit target);
}

public interface IStatus
{
    string DisplayName { get; }
    int DefaultTurns { get; }
    void OnApply(Spirit target);
    void OnExpire(Spirit target);

    // Multipliers are applied when the owner takes actions or receives effects. Return 1f if not used.
    float GetOutgoingDamageMultiplier(Spirit owner);
}
