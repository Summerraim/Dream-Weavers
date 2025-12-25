using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTypeWeight_cza : MonoBehaviour
{
 public int CombatWeight;
    public int RestWeight;
    public int PropsWeight;
    public int SkillWeight;

    public RoomTypeWeight_cza(int c, int r, int p, int e)
    {
        CombatWeight = c;
        RestWeight = r;
        PropsWeight = p;
        SkillWeight = e;
    }

    public int Total => CombatWeight + RestWeight + PropsWeight + SkillWeight;
}
