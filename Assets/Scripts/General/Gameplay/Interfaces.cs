using System.Collections.Generic;

public interface IBattleUnit
{
    string DisplayName { get; }
    int MaxHP { get; }
    int HP { get; }
    int MaxMana { get; }
    int Mana { get; }
    int Damage { get; }
    int Defense { get; }
    bool IsDead { get; }
    IReadOnlyList<ISkill> GetSkills();
    void ReceiveDamage(int amount);
    void ReceiveHeal(int amount);
}

public interface ISkill
{
    string DisplayName { get; }
    int ManaCost { get; }
    int CooldownTurns { get; }
    void Execute(IBattleUnit caster, IBattleUnit target);
}
