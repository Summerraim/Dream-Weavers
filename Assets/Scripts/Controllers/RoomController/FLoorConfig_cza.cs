using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloorConfig_cza
{
    public static int GetRoomCount(int floor)
    {
        switch (floor)
        {
            case 1: return 7;
            case 2: return 6;
            case 3: return 6;
            case 4: return 5;
        }
        return 6;
    }

    public static int GetRestRoomCount(int floor)
    {
        if (floor == 1)
            return 1; // 稍后处理“最多 2”逻辑
        return 1;
    }
}