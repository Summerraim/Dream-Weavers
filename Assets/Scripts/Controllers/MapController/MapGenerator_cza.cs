using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator_cza
{
    // 调试开关
    public static bool DebugEnabled = true;

    public static FloorMap_cza GenerateFloor(int floor, SeedRNG_cza rng)
    {
        int totalRooms = FloorConfig_cza.GetRoomCount(floor);
        FloorMap_cza map = new FloorMap_cza(floor);

        // 新的数量约束
        int restLimit = (floor == 1) ? rng.NextInt(1, 3) : 1;   // 第1层 1~2，其它层 1
        int propsLimit = (rng.NextInt(0, 2) == 0) ? 0 : 1;      // 原逻辑 0 或 1
        int restCount = 0;
        int propsCount = 0;

        if (DebugEnabled)
            Debug.Log($"[MapGen] 开始生成楼层 {floor}, 房间总数={totalRooms}");

        // 按序生成房间类型 (最后一个为 Boss)
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

                // 计数更新
                if (type == RoomType_cza.Rest) restCount++;
                else if (type == RoomType_cza.Props) propsCount++;

                if (DebugEnabled)
                    Debug.Log($"[MapGen] 房间 {i} 生成类型={type} 当前 Rest={restCount}/{restLimit} Props={propsCount}/{propsLimit}");
            }

            map.Rooms[i] = new RoomNode_cza(i, type);
        }

        // 保证至少1个 Rest
        if (restCount == 0)
        {
            // 选一个非 Boss 房间强制改为 Rest
            int forcedId = rng.NextInt(1, totalRooms); // 可能为 Boss（最后一个），做修正
            if (forcedId == totalRooms) forcedId = 1;  // 避免选到 Boss
            var node = map.Rooms[forcedId];
            // 若原来是 Props 需要回退 propsCount
            if (node.Type == RoomType_cza.Props) propsCount = Mathf.Max(0, propsCount - 1);
            var oldType = node.Type;
            node.Type = RoomType_cza.Rest;
            restCount = 1;
            if (DebugEnabled)
                Debug.Log($"[MapGen] 没有生成 Rest，强制将房间 {forcedId} 从 {oldType} 改为 Rest 以满足最少1个要求");
        }

        if (DebugEnabled)
            Debug.Log($"[MapGen] 限制: RestLimit={restLimit}, PropsLimit={propsLimit} 实际: Rest={restCount}, Props={propsCount}");
        // 路线生成 (恢复原逻辑)
        for (int i = 1; i <= totalRooms; i++)
        {
            RoomNode_cza node = map.Rooms[i];
            if (node.Type == RoomType_cza.Boss)
            {
                if (DebugEnabled) Debug.Log($"[MapGen] 房间 {i} 为 Boss，跳过路径生成");
                continue;
            }
            List<int> nextPossibleRooms = new List<int>();
            for (int j = i + 1; j <= totalRooms; j++)
                nextPossibleRooms.Add(j);
            if (DebugEnabled)
                Debug.Log($"[MapGen] 房间 {i} 可选后继: {string.Join(",", nextPossibleRooms)}");
            if (nextPossibleRooms.Count <= 1)
            {
                int target = nextPossibleRooms[0];
                node.NextRooms.Add(target);
                node.NextRooms.Add(target);
                node.NextRooms.Add(target);
                if (DebugEnabled)
                    Debug.Log($"[MapGen] 房间 {i} 只有一个后继 {target}，重复填充3次");
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
            if (DebugEnabled)
                Debug.Log($"[MapGen] 房间 {i} 最终静态前向分支(基础图): {string.Join(",", node.NextRooms)}");
        }

        if (DebugEnabled)
            Debug.Log($"[MapGen] 楼层 {floor} 生成完成");
            Debug.Log($"[MapGen] Rooms.Count={map.Rooms.Count} Keys={string.Join(",", map.Rooms.Keys)}");
        return map;
    }

    // 移除: GenerateRestRooms 与 GeneratePropsRoom (逻辑已由权重约束替代)
}
