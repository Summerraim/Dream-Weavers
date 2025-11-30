using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomNode_cza : MonoBehaviour
{
     public int Id;
    public RoomType_cza Type;
    public List<int> NextRooms;

    public RoomNode_cza(int id, RoomType_cza type)
    {
        Id = id;
        Type = type;
        NextRooms = new List<int>();
    }
}
