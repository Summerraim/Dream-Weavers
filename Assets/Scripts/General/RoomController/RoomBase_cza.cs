using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoomBase_cza : MonoBehaviour
{
public string RoomID;
    public RoomType_cza Type;

    // 进入房间
    public abstract void EnterRoom();

    // 离开房间（例如战斗结束）
    public abstract void ExitRoom();
}
