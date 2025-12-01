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
    public string DisplayName =>
        data != null
            ? (string.IsNullOrWhiteSpace(data.DisplayName) ? data.name : data.DisplayName)
            : string.Empty;
    public int MaxHP => data?.MaxHP ?? 0;
    public int MaxMana => data?.MaxMana ?? 0;

    public Spirit(SpiritData data)
    {
        this.data = data;
        HP = data.MaxHP;
        Mana = data.MaxMana;
        Damage = Mathf.Max(0, data.Damage);
        Defense = Mathf.Max(0, data.Defense);
    }

    public IReadOnlyList<ISkill> GetSkills()
    {
        var list = new List<ISkill>();
        if (Data != null && Data.Skills != null)
        {
            for (int i = 0; i < Data.Skills.Length; i++)
            {
                if (Data.Skills[i] is ISkill skill)
                    list.Add(skill);
            }
        }
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
        HP = Mathf.Min(Data.MaxHP, HP + Mathf.Max(0, v));
    }
}
