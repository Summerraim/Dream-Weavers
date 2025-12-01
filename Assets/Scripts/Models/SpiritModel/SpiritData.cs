using UnityEngine;

[CreateAssetMenu(menuName = "Data/Spirit")]
public class SpiritData : ScriptableObject
{
    public string DisplayName;
    public int MaxHP = 1000;
    public int MaxMana = 100;
    public int Damage = 50;
    public int Defense = 10;

    public Skill[] Skills;
}
