using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIActions_cza : MonoBehaviour
{
    [Header("显示")]
    [SerializeField] private TextMeshProUGUI currentRoomText;
    [SerializeField] private TextMeshProUGUI nextRoomsText;

    [Header("按钮")]
    [SerializeField] private Button btnComplete;
    [SerializeField] private Button btnNext1;
    [SerializeField] private Button btnNext2;
    [SerializeField] private Button btnNext3;
    // [SerializeField] private Button btnRandom;
    [SerializeField] private Button btnReInit; // 可选

    // [SerializeField] private int reinitFloor = 1;

    private void Awake()
    {
        // 绑定按钮
        if (btnComplete) btnComplete.onClick.AddListener(() => RoomStateMachine_cza.Instance.CompleteCurrentRoom());
        if (btnNext1) btnNext1.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToNext(0));
        if (btnNext2) btnNext2.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToNext(1));
        if (btnNext3) btnNext3.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToNext(2));
        // if (btnRandom) btnRandom.onClick.AddListener(() => RoomStateMachine_cza.Instance.GoToRandomNext());
        // if (btnReInit) btnReInit.onClick.AddListener(() => RoomStateMachine_cza.Instance.InitFloor(reinitFloor));
    }

    private void OnEnable()
    {
        if (RoomStateMachine_cza.Instance != null)
            RoomStateMachine_cza.Instance.OnRoomEntered += RefreshRoomInfo;
    }

    private void OnDisable()
    {
        if (RoomStateMachine_cza.Instance != null)
            RoomStateMachine_cza.Instance.OnRoomEntered -= RefreshRoomInfo;
    }

    private void RefreshRoomInfo(RoomNode_cza node)
    {
        if (currentRoomText)
            currentRoomText.text = $"房间 {node.Id} / 类型 {node.Type}";

        if (nextRoomsText)
        {
            if (node.NextRooms == null || node.NextRooms.Count == 0)
                nextRoomsText.text = "后续: 无 (Boss 或末尾)";
            else
                nextRoomsText.text = $"分支: {FormatList(node.NextRooms)}";
        }

        // 按钮可用性
        bool hasNext = node.NextRooms != null && node.NextRooms.Count > 0;
        if (btnNext1) btnNext1.interactable = hasNext && node.NextRooms.Count >= 1;
        if (btnNext2) btnNext2.interactable = hasNext && node.NextRooms.Count >= 2;
        if (btnNext3) btnNext3.interactable = hasNext && node.NextRooms.Count >= 3;
        // if (btnRandom) btnRandom.interactable = hasNext;
    }

    private string FormatList(System.Collections.Generic.List<int> list)
    {
        return string.Join(", ", list);
    }
}