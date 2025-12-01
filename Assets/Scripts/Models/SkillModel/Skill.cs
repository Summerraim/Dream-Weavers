using System;
using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    public string Title => data != null ? data.name : string.Empty;

    public string Description => data != null ? data.Description : string.Empty;

    public Sprite Image => data != null ? data.Image : null;

    public IReadOnlyList<Effect> Effects => data?.Effects ?? Array.Empty<Effect>();

    public int Mana => data?.Mana ?? 0;

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
