using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Skill")]
public class SkillData : ScriptableObject, ISkill
{
    [SerializeField]
    private string displayNameOverride;

    [field: SerializeField]
    public string Description { get; private set; }

    [field: SerializeField]
    public int Mana { get; private set; }

    [field: SerializeField, Min(0)]
    public int CooldownTurns { get; private set; }

    [field: SerializeField, Min(0)]
    public int MaxUsesPerBattle { get; private set; } = 0; // 0表示无限制

    [field: SerializeField]
    public Sprite Image { get; private set; }

    [field: SerializeField]
    public AnimationClip SkillAnimation { get; private set; }

    [SerializeField]
    private List<Effect> effects = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayNameOverride) ? name : displayNameOverride;
    public int ManaCost => Mana;
    public IReadOnlyList<Effect> Effects => effects ?? (effects = new List<Effect>());

    public void Execute(IBattleUnit caster, IBattleUnit target)
    {
        if (effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            effect.Apply(caster, target);
        }
    }
}
