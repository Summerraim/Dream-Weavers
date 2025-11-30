using TMPro;
using UnityEngine;

public class SkillView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title;

    [SerializeField]
    private TMP_Text description;

    [SerializeField]
    private TMP_Text mana;

    [SerializeField]
    private GameObject wrapper;

    public Skill Skill { get; private set; }

    public void Setup(Skill skill)
    {
        Skill = skill;
        title.text = skill.Title;
        description.text = skill.Description;
        mana.text = skill.Mana.ToString();
    }
}
