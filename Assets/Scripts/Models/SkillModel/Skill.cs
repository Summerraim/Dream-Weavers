using System;
using System.Collections.Generic;
using UnityEngine;

public class Skill : ISkill
{
    public string DisplayName => data != null ? data.DisplayName : string.Empty;
    public string Title => data != null ? data.name : string.Empty;

    public string Description => data != null ? data.Description : string.Empty;

    public Sprite Image => data != null ? data.Image : null;

    public IReadOnlyList<Effect> Effects => data?.Effects ?? Array.Empty<Effect>();

    public int Mana => data?.Mana ?? 0;
    public int ManaCost => Mana;
    public int CooldownTurns => data?.CooldownTurns ?? 0;

    private readonly SkillData data;

    public Skill(SkillData skillData)
    {
        data = skillData;
    }

    public void Execute(IBattleUnit caster, IBattleUnit target)
    {
        data?.Execute(caster, target);
    }
}
