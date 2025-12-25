using System.Collections.Generic;
using UnityEngine;

public class Enemy : IBattleUnit
{
    private readonly EnemyData data;
    public EnemyData Data => data;

    public int HP { get; private set; }
    public int Mana { get; private set; }
    public float Damage { get; private set; }
    public float Defense { get; private set; }

    public string DisplayName =>
        data != null
            ? (string.IsNullOrWhiteSpace(data.DisplayName) ? data.name : data.DisplayName)
            : string.Empty;

    public Sprite Image => data?.Image;
    public int MaxHP => data?.MaxHP ?? 0;
    public int MaxMana => data?.MaxMana ?? 0;

    public Enemy(EnemyData data)
    {
        this.data = data;
        HP = MaxHP;
        Mana = MaxMana;
        Damage = Mathf.Max(0, data.Damage);
        Defense = Mathf.Max(0, data.Defense);
    }

    public IReadOnlyList<ISkill> GetSkills()
    {
        var list = new List<ISkill>();
        if (data != null && data.Skills != null)
        {
            UnityEngine.Debug.Log($"Enemy.GetSkills: Found {data.Skills.Length} skill objects");
            for (int i = 0; i < data.Skills.Length; i++)
            {
                var skillObj = data.Skills[i];
                UnityEngine.Debug.Log($"Enemy.GetSkills[{i}]: Type={skillObj?.GetType().Name ?? "null"}, IsISkill={(skillObj is ISkill)}, IsSkillData={(skillObj is SkillData)}");
                
                if (skillObj == null)
                    continue;

                // 尝试直接作为 ISkill
                if (skillObj is ISkill skill)
                {
                    UnityEngine.Debug.Log($"Enemy.GetSkills[{i}]: Added as direct ISkill");
                    list.Add(skill);
                    continue;
                }

                // 如果是 SkillData，用 Skill 类包装
                if (skillObj is SkillData skillData)
                {
                    UnityEngine.Debug.Log($"Enemy.GetSkills[{i}]: Wrapping SkillData with Skill class");
                    list.Add(new Skill(skillData));
                }
            }
        }
        UnityEngine.Debug.Log($"Enemy.GetSkills: Returning {list.Count} skills");
        return list;
    }

    /// <summary>
    /// 敌人死亡判定：HP <= 0 或 Mana <= 0 都视为死亡
    /// </summary>
    public bool IsDead => HP <= 0 || Mana <= 0;

    public void ReceiveDamage(int dmg)
    {
        int incoming = Mathf.Max(0, dmg);
        if (incoming == 0)
            return;

        float reduction = 1f;
        float denominator = Defense + 20f;
        if (denominator > 0f)
        {
            reduction = Mathf.Clamp01(1f - (Defense / denominator));
        }

        int finalDamage = Mathf.CeilToInt(incoming * reduction);

        var battle = BattleModel.ActiveBattle;
        int hpDamage = battle != null ? battle.ModifyDamageReceived(this, finalDamage) : finalDamage;
        hpDamage = Mathf.Max(0, hpDamage);

        if (hpDamage > 0)
        {
            HP = Mathf.Max(0, HP - hpDamage);
        }

        battle?.NotifyDamageReceived(this, hpDamage);
    }

    public void ReceiveHeal(int v)
    {
        HP = Mathf.Min(MaxHP, HP + Mathf.Max(0, v));
    }

    public void ConsumeMana(int amount)
    {
        Mana = Mathf.Max(0, Mana - Mathf.Max(0, amount));
    }

    public void ReceiveMana(int amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + Mathf.Max(0, amount));
    }
}
