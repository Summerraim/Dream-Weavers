using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator_cza
{
    public static bool DebugEnabled = false;

    public static FloorMap_cza GenerateFloor(int floor, SeedRNG_cza rng)
    {
        int totalRooms = FloorConfig_cza.GetRoomCount(floor);
        FloorMap_cza map = new FloorMap_cza(floor);

        int restLimit = (floor == 1) ? rng.NextInt(1, 3) : 1;
        int propsLimit = (rng.NextInt(0, 2) == 0) ? 0 : 1;
        int restCount = 0;
        int propsCount = 0;

        Debug.Log($"[MapGen] Start generating floor {floor}, total rooms {totalRooms}");

        for (int i = 1; i <= totalRooms; i++)
        {
            RoomType_cza type;
            if (i == totalRooms)
            {
                type = RoomType_cza.Boss;
            }
            else
            {
                bool allowRest = restCount < restLimit;
                bool allowProps = propsCount < propsLimit;

                type = RoomType_czaGenerator_cza.GenerateRoomType_czaWithConstraints(
                    floor,
                    rng,
                    allowRest,
                    allowProps
                );

                if (type == RoomType_cza.Rest)
                    restCount++;
                else if (type == RoomType_cza.Props)
                    propsCount++;
            }

            map.Rooms[i] = new RoomNode_cza(i, type);
        }

        // Floor 1 keeps its random opener. Later floors always start in a Rest room.
        if (floor > 1 && map.Rooms.TryGetValue(1, out RoomNode_cza startNode) && startNode != null)
        {
            if (startNode.Type == RoomType_cza.Props)
                propsCount = Mathf.Max(0, propsCount - 1);

            if (startNode.Type != RoomType_cza.Rest)
            {
                RoomType_cza oldType = startNode.Type;
                startNode.Type = RoomType_cza.Rest;
                restCount++;

                if (DebugEnabled)
                    Debug.Log($"[MapGen] Floor {floor} forces room 1 from {oldType} to Rest");
            }
        }

        if (restCount == 0)
        {
            int forcedId = rng.NextInt(1, totalRooms);
            if (forcedId == totalRooms)
                forcedId = 1;

            RoomNode_cza node = map.Rooms[forcedId];
            if (node.Type == RoomType_cza.Props)
                propsCount = Mathf.Max(0, propsCount - 1);

            RoomType_cza oldType = node.Type;
            node.Type = RoomType_cza.Rest;
            restCount = 1;

            if (DebugEnabled)
                Debug.Log($"[MapGen] No Rest generated, force room {forcedId} from {oldType} to Rest");
        }

        int skillCount = 0;
        foreach (KeyValuePair<int, RoomNode_cza> kv in map.Rooms)
        {
            if (kv.Value.Type == RoomType_cza.Skill)
                skillCount++;
        }

        if (skillCount == 0)
        {
            List<int> candidates = new List<int>();
            for (int i = 1; i < totalRooms; i++)
            {
                if (map.Rooms[i].Type != RoomType_cza.Rest)
                    candidates.Add(i);
            }

            if (candidates.Count > 0)
            {
                int forcedId = candidates[rng.NextInt(0, candidates.Count)];
                RoomNode_cza node = map.Rooms[forcedId];
                if (node.Type == RoomType_cza.Props)
                    propsCount = Mathf.Max(0, propsCount - 1);

                RoomType_cza oldType = node.Type;
                node.Type = RoomType_cza.Skill;
                skillCount = 1;

                if (DebugEnabled)
                    Debug.Log($"[MapGen] No Skill generated, force room {forcedId} from {oldType} to Skill");
            }
        }

        for (int i = 1; i <= totalRooms; i++)
        {
            RoomNode_cza node = map.Rooms[i];
            if (node.Type == RoomType_cza.Boss)
                continue;

            List<int> nextPossibleRooms = new List<int>();
            for (int j = i + 1; j <= totalRooms; j++)
                nextPossibleRooms.Add(j);

            if (nextPossibleRooms.Count <= 1)
            {
                int target = nextPossibleRooms[0];
                node.NextRooms.Add(target);
                node.NextRooms.Add(target);
                node.NextRooms.Add(target);
            }
            else
            {
                int pickCount = System.Math.Min(3, nextPossibleRooms.Count);
                for (int k = 0; k < pickCount; k++)
                {
                    int index = rng.NextInt(0, nextPossibleRooms.Count);
                    node.NextRooms.Add(nextPossibleRooms[index]);
                    nextPossibleRooms.RemoveAt(index);
                }

                while (node.NextRooms.Count < 3)
                    node.NextRooms.Add(node.NextRooms[0]);
            }
        }

        Debug.Log($"[MapGen] Floor {floor} generated, rooms={map.Rooms.Count}");
        return map;
    }
}
