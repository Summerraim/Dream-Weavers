using System.Collections.Generic;
using UnityEngine;

public class Spirit
{
    private readonly SpiritData data;
    public SpiritData Data => data;
    public int HP { get; private set; }
    public int Mana { get; private set; }

    class ActiveStatus
    {
        public IStatus Asset;
        public int Turns;
    }

    readonly List<ActiveStatus> statuses = new List<ActiveStatus>();

    public Spirit(SpiritData data)
    {
        this.data = data;
        HP = data.MaxHP;
        Mana = data.MaxMana;
    }

    public List<ISkill> GetSkills()
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

    public void ApplyStatus(IStatus statusAsset, int turns = -1)
    {
        if (statusAsset == null)
            return;
        int t = (turns > 0) ? turns : statusAsset.DefaultTurns;
        statuses.Add(new ActiveStatus { Asset = statusAsset, Turns = t });
        statusAsset.OnApply(this);
    }

    public float GetOutgoingDamageMultiplier()
    {
        float mul = 1f;
        for (int i = 0; i < statuses.Count; i++)
        {
            var s = statuses[i];
            if (s.Turns > 0)
                mul *= s.Asset.GetOutgoingDamageMultiplier(this);
        }
        return Mathf.Max(0f, mul);
    }

    public void ReceiveDamage(int dmg)
    {
        HP = Mathf.Max(0, HP - Mathf.Max(0, dmg));
    }

    public void ReceiveHeal(int v)
    {
        HP = Mathf.Min(Data.MaxHP, HP + Mathf.Max(0, v));
    }
}
