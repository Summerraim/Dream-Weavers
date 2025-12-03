using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTypeWeight_cza : MonoBehaviour
{
 public int CombatWeight;
    public int RestWeight;
    public int PropsWeight;
    public int EventWeight;

    public RoomTypeWeight_cza(int c, int r, int p, int e)
    {
        CombatWeight = c;
        RestWeight = r;
        PropsWeight = p;
        EventWeight = e;
    }

    public int Total => CombatWeight + RestWeight + PropsWeight + EventWeight;
}
