using System.Collections.Generic;
using UnityEngine;

public class Spirit : IBattleUnit
{
    private readonly SpiritData data;
    public SpiritData Data => data;
    public int HP { get; private set; }
    public int Mana { get; private set; }
    public int Damage { get; private set; }
    public int Defense { get; private set; }
    private readonly int baseMaxHP;
    private readonly int baseMaxMana;
    private float bonusMaxHpPercent;
    private readonly int baseDamage;
    private readonly int baseDefense;
    private readonly List<SynergyModel> synergyModels = new List<SynergyModel>();
    public string DisplayName =>
        data != null
            ? (string.IsNullOrWhiteSpace(data.DisplayName) ? data.name : data.DisplayName)
            : string.Empty;
    public int MaxHP => Mathf.CeilToInt(baseMaxHP * (1f + bonusMaxHpPercent));
    public int MaxMana => baseMaxMana;
    public float MaxHpBonusPercent => bonusMaxHpPercent;
    public int BaseMaxHP => baseMaxHP;
    public int BaseMaxMana => baseMaxMana;
    public int BaseDamage => baseDamage;
    public int BaseDefense => baseDefense;
    public IReadOnlyList<SynergyModel> Synergies => synergyModels;

    public Spirit(SpiritData data)
    {
        this.data = data;
        baseMaxHP = data.MaxHP;
        baseMaxMana = data.MaxMana;
        HP = MaxHP;
        Mana = baseMaxMana;
        baseDamage = Mathf.Max(0, data.Damage);
        baseDefense = Mathf.Max(0, data.Defense);
        Damage = baseDamage;
        Defense = baseDefense;

        if (data.Synergies != null)
        {
            for (int i = 0; i < data.Synergies.Length; i++)
            {
                var synergy = data.Synergies[i];
                if (synergy == null)
                    continue;
                synergyModels.Add(new SynergyModel(this, synergy));
            }
        }
    }

    public IReadOnlyList<ISkill> GetSkills()
    {
        var list = new List<ISkill>();
        if (Data != null && Data.Skills != null)
        {
            UnityEngine.Debug.Log($"Spirit.GetSkills: Found {Data.Skills.Length} skill objects");
            for (int i = 0; i < Data.Skills.Length; i++)
            {
                var skillObj = Data.Skills[i];
                UnityEngine.Debug.Log(
                    $"Spirit.GetSkills[{i}]: Type={skillObj?.GetType().Name ?? "null"}, IsISkill={(skillObj is ISkill)}, IsSkillData={(skillObj is SkillData)}"
                );

                if (skillObj == null)
                    continue;

                // 尝试直接作为 ISkill
                if (skillObj is ISkill skill)
                {
                    UnityEngine.Debug.Log($"Spirit.GetSkills[{i}]: Added as direct ISkill");
                    list.Add(skill);
                    continue;
                }

                // 如果是 SkillData，用 Skill 类包装
                if (skillObj is SkillData skillData)
                {
                    UnityEngine.Debug.Log(
                        $"Spirit.GetSkills[{i}]: Wrapping SkillData with Skill class"
                    );
                    list.Add(new Skill(skillData));
                }
            }
        }
        UnityEngine.Debug.Log($"Spirit.GetSkills: Returning {list.Count} skills");
        return list;
    }

    public bool IsDead => HP <= 0;

    public void ReceiveDamage(int damage)
    {
        int incoming = Mathf.Max(0, damage);
        if (incoming == 0)
            return;

        float reduction = 1f;
        float denominator = Defense + 10f;
        if (denominator > 0f)
        {
            reduction = Mathf.Clamp01(1f - (Defense / denominator));
        }

        int finalDamage = Mathf.CeilToInt(incoming * reduction);
        HP = Mathf.Max(0, HP - Mathf.Max(0, finalDamage));
    }

    public void ReceiveHeal(int v)
    {
        HP = Mathf.Min(MaxHP, HP + Mathf.Max(0, v));
    }

    public void ConsumeMana(int amount)
    {
        Mana = Mathf.Max(0, Mana - Mathf.Max(0, amount));
    }

    public void SetMaxHpBonusPercent(float percent)
    {
        bonusMaxHpPercent = Mathf.Max(0f, percent);
        HP = Mathf.Min(HP, MaxHP);
    }
}
