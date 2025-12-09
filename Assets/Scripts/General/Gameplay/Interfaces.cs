using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleUnit
{
    string DisplayName { get; }
    Sprite Image { get; }
    int MaxHP { get; }
    int HP { get; }
    int MaxMana { get; }
    int Mana { get; }
    float Damage { get; }
    float Defense { get; }
    bool IsDead { get; }
    IReadOnlyList<ISkill> GetSkills();
    void ReceiveDamage(int amount);
    void ReceiveHeal(int amount);
    void ConsumeMana(int amount);
}

public interface ISkill
{
    string DisplayName { get; }
    string Description { get; }
    int ManaCost { get; }
    int CooldownTurns { get; }
    int MaxUsesPerBattle { get; } // 每场战斗最大使用次数，0表示无限制
    void Execute(IBattleUnit caster, IBattleUnit target);
}

// 一次性消耗型道具接口：无法力消耗/冷却，随时可用，由背包在成功使用后扣减数量
public interface IItem
{
    // 标识与显示
    string ItemId { get; }
    string DisplayName { get; }

    // 叠加信息（便于与背包模型对齐，但不涉及资源消耗/冷却）
    int MaxStack { get; }
    bool RemoveOnUse { get; } // 通常为 true，表示使用后消耗一份

    // 使用判定与实际生效
    bool CanUse(IBattleUnit user, IBattleUnit target);
    void Use(IBattleUnit user, IBattleUnit target);
}

// 兼容旧命名：建议改用 IItem
[Obsolete("Use IItem instead")]
public interface Items : IItem { }
