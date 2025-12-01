using System.Collections.Generic;

public class RoomNode_cza
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
