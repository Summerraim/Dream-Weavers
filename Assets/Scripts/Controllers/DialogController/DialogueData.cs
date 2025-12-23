using UnityEngine;

/// <summary>
/// 对话条目
/// </summary>
[System.Serializable]
public class DialogueEntry
{
    public string speakerName;                    // 说话者名字
    [TextArea(3, 5)] public string dialogueText;  // 对话文本
    public Sprite portrait;                       // 角色立绘
    public UI_DialogView.PortraitPosition portraitPosition = UI_DialogView.PortraitPosition.Left; // 立绘位置
}

/// <summary>
/// 对话数据
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;                     // 对话ID
    public DialogueEntry[] dialogueEntries;       // 对话条目数组
}
