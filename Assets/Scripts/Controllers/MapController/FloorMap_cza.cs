using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorMap_cza
{
    public int FloorIndex;
    public Dictionary<int, RoomNode_cza> Rooms;

    public FloorMap_cza(int floor)
    {
        FloorIndex = floor;
        Rooms = new Dictionary<int, RoomNode_cza>();
    }
}
