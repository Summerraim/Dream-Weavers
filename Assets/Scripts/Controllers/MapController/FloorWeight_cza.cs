using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloorWeight_cza
{
    public static RoomTypeWeight_cza GetWeights(int floor)
    {
        switch (floor)
        {
            case 1:
                return new RoomTypeWeight_cza(
                    c: 70,   // 战斗权重最高
                    r: 10,
                    p: 10,
                    e: 10
                );
            case 2:
            case 3:
                return new RoomTypeWeight_cza(
                    c: 35,   // 战斗降低
                    r: 20,
                    p: 25,
                    e: 20
                );
            case 4:
                return new RoomTypeWeight_cza(
                    c: 50,   // 略微升高
                    r: 15,
                    p: 20,
                    e: 15
                );
        }
        return null;
    }
}