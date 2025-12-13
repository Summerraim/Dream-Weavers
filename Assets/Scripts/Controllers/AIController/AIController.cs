using System.Collections.Generic;

public class AIController
{
    private readonly System.Random random = new System.Random();

    // 敌人技能索引偏移量，避免与玩家技能索引冲突
    private const int ENEMY_SKILL_INDEX_OFFSET = 100;

    /// <summary>
    /// 决定敌人使用哪个技能
    /// </summary>
    /// <param name="enemy">敌人单位</param>
    /// <param name="player">玩家单位</param>
    /// <param name="model">战斗模型（用于检查冷却和使用次数）</param>
    /// <param name="outSkillIndex">输出选中技能的实际索引</param>
    /// <returns>选中的技能，如果没有可用技能返回null</returns>
    public ISkill DecideSkill(Enemy enemy, Spirit player, BattleModel model, out int outSkillIndex)
    {
        outSkillIndex = -1;

        var skills = enemy?.GetSkills();
        if (enemy == null || player == null || skills == null || skills.Count == 0)
            return null;

        var available = new List<(ISkill skill, int index)>();

        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill == null)
                continue;

            // 检查法力是否足够
            if (enemy.Mana < skill.ManaCost)
                continue;

            // 使用偏移后的索引检查冷却和使用次数
            int enemySkillIndex = ENEMY_SKILL_INDEX_OFFSET + i;

            // 检查是否在冷却中
            if (model != null && model.IsSkillOnCooldown(enemySkillIndex))
                continue;

            // 检查是否达到使用次数上限
            if (model != null && model.IsSkillUsageLimitReached(enemySkillIndex, skill))
                continue;

            available.Add((skill, i));
        }

        if (available.Count == 0)
            return null;

        int selectedIndex = random.Next(available.Count);
        var selected = available[selectedIndex];
        outSkillIndex = selected.index;
        return selected.skill;
    }

    /// <summary>
    /// 旧版本兼容方法（不检查冷却和使用次数）
    /// </summary>
    [System.Obsolete("Use DecideSkill with BattleModel parameter instead")]
    public ISkill DecideSkill(Enemy enemy, Spirit player)
    {
        int dummy;
        return DecideSkill(enemy, player, null, out dummy);
    }

    /// <summary>
    /// 获取敌人技能的存储索引（添加偏移量以避免与玩家技能冲突）
    /// </summary>
    public static int GetEnemySkillIndex(int skillIndex)
    {
        return ENEMY_SKILL_INDEX_OFFSET + skillIndex;
    }
}
