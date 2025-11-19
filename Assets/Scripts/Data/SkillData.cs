using DW.Data;
using UnityEngine;

public enum SkillEffectType
{
    Damage = 0,
    ApplyWeaken = 1,
    ApplyEnhance = 2,
}

// One skill can have multiple effects.
[System.Serializable]
public class SkillEffect
{
    public SkillEffectType Type = SkillEffectType.Damage;

    // Damage params
    public int Power = 100;

    // ApplyWeaken params (inline)
    public float WeakenRatio = 0.2f; // 20% reduce outgoing damage
    public int WeakenTurns = 2;

    // ApplyEnhance params (inline)
    public float EnhanceRatio = 0.2f; // 20% increase outgoing damage
    public int EnhanceTurns = 2;
}

[CreateAssetMenu(menuName = "Skill/Skill Data", fileName = "SkillData")]
public class SkillData : ScriptableObject, ISkill
{
    public string DisplayName;

    public int ManaCost = 10;
    public int CooldownTurns = 1;

    // New multi-effect list
    public SkillEffect[] Effects = new SkillEffect[0];

    string ISkill.DisplayName => DisplayName;
    int ISkill.ManaCost => ManaCost;
    int ISkill.CooldownTurns => CooldownTurns;

    // Inline status used to apply Weaken/Enhance without separate assets
    private sealed class InlineStatus : IStatus
    {
        private readonly string display;
        private readonly StatusType type;
        private readonly float ratio;
        private readonly int defaultTurns;

        public InlineStatus(string name, StatusType type, float ratio, int turns)
        {
            this.display = name;
            this.type = type;
            this.ratio = ratio;
            this.defaultTurns = turns;
        }

        string IStatus.DisplayName => display;
        int IStatus.DefaultTurns => defaultTurns;
        void IStatus.OnApply(Spirit target) { }
        void IStatus.OnExpire(Spirit target) { }
        float IStatus.GetOutgoingDamageMultiplier(Spirit owner)
        {
            switch (type)
            {
                case StatusType.Weaken:
                {
                    float weak = 1f - Mathf.Clamp(ratio, 0f, 1f);
                    return Mathf.Max(0f, weak);
                }
                case StatusType.Enhance:
                {
                    return 1f + Mathf.Max(0f, ratio);
                }
            }
            return 1f;
        }
    }

    void ISkill.Execute(Spirit caster, Spirit target)
    {
        if (caster == null || target == null)
            return;

        if (Effects == null || Effects.Length == 0)
            return;

        for (int i = 0; i < Effects.Length; i++)
        {
            var e = Effects[i];
            switch (e.Type)
            {
                case SkillEffectType.Damage:
                {
                    int dmg = Mathf.Max(
                        0,
                        Mathf.RoundToInt((float)e.Power * caster.GetOutgoingDamageMultiplier())
                    );
                    target.ReceiveDamage(dmg);
                    break;
                }
                case SkillEffectType.ApplyWeaken:
                {
                    int turns = (e.WeakenTurns > 0) ? e.WeakenTurns : 1;
                    var status = new InlineStatus("Weaken", StatusType.Weaken, e.WeakenRatio, turns);
                    target.ApplyStatus(status, turns);
                    break;
                }
                case SkillEffectType.ApplyEnhance:
                {
                    int turns = (e.EnhanceTurns > 0) ? e.EnhanceTurns : 1;
                    var status = new InlineStatus("Enhance", StatusType.Enhance, e.EnhanceRatio, turns);
                    // Enhance 应当作用于施法者自身，提高其输出伤害
                    caster.ApplyStatus(status, turns);
                    break;
                }
            }
        }
    }
}
