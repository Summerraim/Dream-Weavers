using UnityEngine;

[CreateAssetMenu(menuName = "Data/Spirit")]
public class SpiritData : ScriptableObject
{
    public string DisplayName;
    public string Description;

    public Sprite Image;
    public int MaxHP = 200;
    public int MaxMana = 125;
    public int Damage = 25;
    public int Defense = 5;

    public ScriptableObject[] Skills;
    public Synergy[] Synergies;
}
