using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    public string DisplayName;
    public Sprite Image;
    public int MaxHP = 500;
    public int MaxMana = 200;
    public int Damage = 30;
    public int Defense = 10;

    public ScriptableObject[] Skills;
}
