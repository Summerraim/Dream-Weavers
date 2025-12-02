using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    public string DisplayName;
    public int MaxHP = 1000;
    public int MaxMana = 100;
    public int Damage = 50;
    public int Defense = 10;

    public ScriptableObject[] Skills;
}
