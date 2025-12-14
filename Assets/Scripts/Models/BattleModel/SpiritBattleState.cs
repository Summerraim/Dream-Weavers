using System.Collections.Generic;

/// <summary>
/// 保存单个Spirit在战斗中的状态（技能选择、冷却、使用次数等）
/// </summary>
public class SpiritBattleState
{
    /// <summary>
    /// 已选择的战斗技能列表（保持顺序）
    /// </summary>
    public List<ISkill> SelectedSkills { get; private set; }

    /// <summary>
    /// 技能索引 -> 剩余冷却回合数
    /// </summary>
    public Dictionary<int, int> SkillCooldowns { get; private set; }

    /// <summary>
    /// 技能索引 -> 当前战斗已使用次数
    /// </summary>
    public Dictionary<int, int> SkillUsageCount { get; private set; }

    public SpiritBattleState()
    {
        SelectedSkills = new List<ISkill>();
        SkillCooldowns = new Dictionary<int, int>();
        SkillUsageCount = new Dictionary<int, int>();
    }

    /// <summary>
    /// 创建带有预选技能的状态
    /// </summary>
    public SpiritBattleState(List<ISkill> selectedSkills)
    {
        SelectedSkills = new List<ISkill>(selectedSkills);
        SkillCooldowns = new Dictionary<int, int>();
        SkillUsageCount = new Dictionary<int, int>();
    }

    /// <summary>
    /// 复制状态（用于保存当前状态）
    /// </summary>
    public SpiritBattleState Clone()
    {
        var clone = new SpiritBattleState();
        clone.SelectedSkills = new List<ISkill>(SelectedSkills);
        clone.SkillCooldowns = new Dictionary<int, int>(SkillCooldowns);
        clone.SkillUsageCount = new Dictionary<int, int>(SkillUsageCount);
        return clone;
    }
}
