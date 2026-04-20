using UnityEngine;

[System.Serializable]
public class DialogueEntry
{
    public string speakerName;
    [TextArea(3, 5)] public string dialogueText;
    public Sprite portrait;
    public UI_DialogView.PortraitPosition portraitPosition = UI_DialogView.PortraitPosition.Left;
    public bool openSpiritPanelOnContinue;
}

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;
    public DialogueEntry[] dialogueEntries;
    public EnemyData associatedEnemy;
    public string enemyNameFilter;
}
