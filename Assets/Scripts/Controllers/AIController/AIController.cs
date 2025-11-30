using System.Collections.Generic;

public class AIController
{
    private readonly System.Random random = new System.Random();

    public ISkill DecideSkill(Enemy enemy, Spirit player)
    {
        var skills = enemy?.GetSkills();
        if (enemy == null || player == null || skills == null || skills.Count == 0)
            return null;

        var available = new List<ISkill>();
        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill == null)
                continue;
            if (enemy.Mana >= skill.ManaCost)
                available.Add(skill);
        }

        if (available.Count == 0)
            return null;

        int index = random.Next(available.Count);
        return available[index];
    }

    public void TakeTurn(Enemy enemy, Spirit player)
    {
        var skill = DecideSkill(enemy, player);
        if (skill == null)
            return;

        skill.Execute(enemy, player);
    }
}
