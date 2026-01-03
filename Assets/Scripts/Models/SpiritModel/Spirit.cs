using System.Collections.Generic;
using UnityEngine;

public class Spirit : IBattleUnit
{
    private readonly SpiritData data;
    public SpiritData Data => data;
    public int HP { get; private set; }
    public int Mana { get; private set; }
    public float Damage { get; private set; }
    public float Defense { get; private set; }
    private readonly int baseMaxHP;
    private readonly int baseMaxMana;
    private float bonusMaxHpPercent;
    private float bonusMaxManaPercent;
    private readonly int baseDamage;
    private readonly int baseDefense;
    private readonly List<SynergyModel> synergyModels = new List<SynergyModel>();
    private readonly List<ISkill> battleSkills = new List<ISkill>(); // 缓存随机选择的战斗技能
    public string DisplayName =>
        data != null
            ? (string.IsNullOrWhiteSpace(data.DisplayName) ? data.name : data.DisplayName)
            : string.Empty;
    public Sprite Image => data?.Image;
    public int MaxHP => Mathf.CeilToInt(baseMaxHP * (1f + bonusMaxHpPercent));
    public int MaxMana => Mathf.CeilToInt(baseMaxMana * (1f + bonusMaxManaPercent));
    public float MaxHpBonusPercent => bonusMaxHpPercent;
    public float MaxManaBonusPercent => bonusMaxManaPercent;
    public int BaseMaxHP => baseMaxHP;
    public int BaseMaxMana => baseMaxMana;
    public int BaseDamage => baseDamage;
    public int BaseDefense => baseDefense;
    public IReadOnlyList<SynergyModel> Synergies => synergyModels;

    public Spirit(SpiritData data)
        : this(data, null) { }

    /// <summary>
    /// 创建Spirit实例，可选地使用预定义的技能列表
    /// </summary>
    /// <param name="data">Spirit数据</param>
    /// <param name="predefinedSkills">预定义的技能列表（用于恢复战斗状态），为null时随机选择</param>
    public Spirit(SpiritData data, List<ISkill> predefinedSkills)
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

        // 初始化技能：使用预定义列表或随机选择
        InitializeBattleSkills(predefinedSkills);
    }

    /// <summary>
    /// 从所有技能中随机选择最多3个技能作为战斗技能，或使用预定义的技能列表
    /// </summary>
    /// <param name="predefinedSkills">预定义的技能列表，为null时随机选择</param>
    private void InitializeBattleSkills(List<ISkill> predefinedSkills = null)
    {
        battleSkills.Clear();

        // 如果提供了预定义技能列表，直接使用
        if (predefinedSkills != null && predefinedSkills.Count > 0)
        {
            battleSkills.AddRange(predefinedSkills);
            UnityEngine.Debug.Log(
                $"Spirit {DisplayName}: Restored {battleSkills.Count} battle skills from saved state"
            );
            return;
        }

        // 否则，随机选择技能（包含本局运行时追加的技能）
        var skillObjects = SpiritRuntimeSkills.GetAllSkillObjects(data);
        if (data == null || skillObjects.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"Spirit {DisplayName}: No skills available");
            return;
        }

        // 收集所有有效技能
        var allSkills = new List<ISkill>();
        for (int i = 0; i < skillObjects.Count; i++)
        {
            var skillObj = skillObjects[i];
            if (skillObj == null)
                continue;

            // 尝试直接作为 ISkill
            if (skillObj is ISkill skill)
            {
                allSkills.Add(skill);
                continue;
            }

            // 如果是 SkillData，用 Skill 类包装
            if (skillObj is SkillData skillData)
            {
                allSkills.Add(new Skill(skillData));
            }
        }

        if (allSkills.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"Spirit {DisplayName}: No valid skills found");
            return;
        }

        // 随机选择最多3个技能
        int skillCount = Mathf.Min(3, allSkills.Count);

        // 使用Fisher-Yates洗牌算法随机选择
        var selectedIndices = new List<int>();
        for (int i = 0; i < allSkills.Count; i++)
        {
            selectedIndices.Add(i);
        }

        // 打乱顺序
        for (int i = selectedIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = selectedIndices[i];
            selectedIndices[i] = selectedIndices[randomIndex];
            selectedIndices[randomIndex] = temp;
        }

        // 选择前skillCount个
        for (int i = 0; i < skillCount; i++)
        {
            battleSkills.Add(allSkills[selectedIndices[i]]);
            UnityEngine.Debug.Log(
                $"Spirit {DisplayName}: Selected skill {i + 1}/{skillCount}: {allSkills[selectedIndices[i]].DisplayName}"
            );
        }

        UnityEngine.Debug.Log(
            $"Spirit {DisplayName}: Initialized with {battleSkills.Count} battle skills"
        );
    }

    public IReadOnlyList<ISkill> GetSkills()
    {
        // 直接返回缓存的战斗技能列表（已在构造函数中随机选择）
        return battleSkills;
    }

    public bool IsDead => HP <= 0;

    public void ReceiveDamage(int damage, IBattleUnit attacker = null)
    {
        int incoming = Mathf.Max(0, damage);
        if (incoming == 0)
            return;

        float effectiveDefense = Defense;
        // 虚空遗民：攻击/技能无视敌人部分防御力（在受击方这里调整防御计算）
        var activeBattle = BattleModel.ActiveBattle;
        if (attacker != null && activeBattle != null)
        {
            var attackerBuffs = activeBattle.GetBuffsForUnit(attacker);
            for (int i = 0; i < attackerBuffs.Count; i++)
            {
                if (attackerBuffs[i] is VoidExileBuff voidExileBuff)
                {
                    effectiveDefense = Mathf.Max(
                        0f,
                        effectiveDefense * (1f - Mathf.Clamp01(voidExileBuff.IgnoreDefensePercent))
                    );
                    break;
                }
            }
        }

        float reduction = 1f;
        float denominator = effectiveDefense + 20f;
        if (denominator > 0f)
        {
            reduction = Mathf.Clamp01(1f - (effectiveDefense / denominator));
        }

        int finalDamage = Mathf.CeilToInt(incoming * reduction);

        int hpDamage = activeBattle != null ? activeBattle.ModifyDamageReceived(this, finalDamage, attacker) : finalDamage;
        hpDamage = Mathf.Max(0, hpDamage);

        if (hpDamage > 0)
        {
            HP = Mathf.Max(0, HP - hpDamage);
        }

        activeBattle?.NotifyDamageReceived(this, hpDamage, attacker);
    }

    public void ReceiveHeal(int v)
    {
        HP = Mathf.Min(MaxHP, HP + Mathf.Max(0, v));
    }

    public void ConsumeMana(int amount)
    {
        Mana = Mathf.Max(0, Mana - Mathf.Max(0, amount));
    }

    public void RestoreMana(int amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + Mathf.Max(0, amount));
    }

    public void ReceiveMana(int amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + Mathf.Max(0, amount));
    }
    public void SetRuntimeHPMP(int currentHP, int currentMP)
    {
        HP = Mathf.Clamp(currentHP, 0, MaxHP);
        Mana = Mathf.Clamp(currentMP, 0, MaxMana);
    }


    public void SetMaxHpBonusPercent(float percent)
    {
        // 允许正负加成：例如 -0.3 表示降低最大生命值上限30%
        // 下限限制为 -0.9，避免出现过低或为0的上限导致异常
        bonusMaxHpPercent = Mathf.Clamp(percent, -0.9f, 5f);
        HP = Mathf.Min(HP, MaxHP);
    }

    public void SetMaxManaBonusPercent(float percent)
    {
        bonusMaxManaPercent = Mathf.Max(0f, percent);
        Mana = Mathf.Min(Mana, MaxMana);
    }

    public void EnhanceDamage(int amount)
    {
        Damage = baseDamage + Mathf.Max(0, amount) * 0.1f;
    }

    public void EnhanceDefense(int amount)
    {
        Defense = baseDefense + Mathf.Max(0, amount);
    }
}
