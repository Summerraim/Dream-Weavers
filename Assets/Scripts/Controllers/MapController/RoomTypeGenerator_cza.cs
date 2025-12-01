using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomType_czaGenerator_cza
{
    public static RoomType_cza GenerateRoomType_cza(int floor, SeedRNG_cza rng)
    {
        RoomTypeWeight_cza w = FloorWeight_cza.GetWeights(floor);
        int roll = rng.NextInt(0, w.Total);

        if (roll < w.CombatWeight)
            return RoomType_cza.Combat;

        roll -= w.CombatWeight;
        if (roll < w.RestWeight)
            return RoomType_cza.Rest;

        roll -= w.RestWeight;
        if (roll < w.PropsWeight)
            return RoomType_cza.Props;

        return RoomType_cza.Events; // 最后剩下的权重
    }

    public static RoomType_cza GenerateRoomType_czaWithConstraints(int floor, SeedRNG_cza rng, bool allowRest, bool allowProps)
    {
        RoomTypeWeight_cza w = FloorWeight_cza.GetWeights(floor);
        int combat = w.CombatWeight;
        int rest = allowRest ? w.RestWeight : 0;
        int props = allowProps ? w.PropsWeight : 0;
        int events = w.Total - (w.CombatWeight + w.RestWeight + w.PropsWeight); // derive events weight from total

        int total = combat + rest + props + events;
        int roll = rng.NextInt(0, total);

        if (roll < combat) return RoomType_cza.Combat;
        roll -= combat;
        if (roll < rest) return RoomType_cza.Rest;
        roll -= rest;
        if (roll < props) return RoomType_cza.Props;
        return RoomType_cza.Events;
    }
}