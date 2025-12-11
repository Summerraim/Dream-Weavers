using UnityEngine;

[CreateAssetMenu(menuName = "Data/Spirit")]
public class SpiritData : ScriptableObject
{
    public string DisplayName;
    public Sprite Image;
    public int MaxHP = 200;
    public int MaxMana = 120;
    public int Damage = 10;
    public int Defense = 5;

    public ScriptableObject[] Skills;
    public Synergy[] Synergies;
}
