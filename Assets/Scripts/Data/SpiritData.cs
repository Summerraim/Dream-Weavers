using UnityEngine;

[CreateAssetMenu(menuName = "Spirit/Spirit Data", fileName = "SpiritData")]
public class SpiritData : ScriptableObject
{
    public string DisplayName;
    public int MaxHP = 1000;
    public int MaxMana = 100;

    // Store concrete ScriptableObjects that iManalement ISkill
    public ScriptableObject[] Skills;
}
