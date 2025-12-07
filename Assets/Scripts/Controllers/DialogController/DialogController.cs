using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogLine
{
    public string speakerName;
    public string dialogText;
    public Sprite speakerPortrait;
    public float displayTime = 3f;
}

public class DialogController : MonoBehaviour
{
    public static DialogController Instance { get; private set; }
    
    [Header("对话UI")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogText;
    public Image portraitImage;
    public GameObject dialogPanel;
    
    private Queue<DialogLine> dialogQueue = new Queue<DialogLine>();
    private Coroutine currentDialogCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    public void ShowDialog(List<DialogLine> dialogLines)
    {
        dialogQueue.Clear();
        
        foreach (var line in dialogLines)
        {
            dialogQueue.Enqueue(line);
        }
        
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        ShowNextLine();
    }
    
    public void ShowNextLine()
    {
        if (currentDialogCoroutine != null)
            StopCoroutine(currentDialogCoroutine);
        
        if (dialogQueue.Count > 0)
        {
            DialogLine line = dialogQueue.Dequeue();
            currentDialogCoroutine = StartCoroutine(DisplayLine(line));
        }
        else
        {
            EndDialog();
        }
    }
    
    private IEnumerator DisplayLine(DialogLine line)
    {
        // 设置说话者信息
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;
        
        if (portraitImage != null && line.speakerPortrait != null)
        {
            portraitImage.sprite = line.speakerPortrait;
            portraitImage.enabled = true;
        }
        
        // 逐字显示对话文本
        if (dialogText != null)
        {
            dialogText.text = "";
            foreach (char c in line.dialogText)
            {
                dialogText.text += c;
                yield return new WaitForSeconds(0.02f); // 打字机效果速度
            }
        }
        
        // 等待一段时间或点击继续
        float timer = 0;
        while (timer < line.displayTime)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                break;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        ShowNextLine();
    }
    
    private void EndDialog()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }
    
    // 快捷方法：显示单句对话
    public void ShowQuickDialog(string speaker, string text, float duration = 3f)
    {
        DialogLine line = new DialogLine
        {
            speakerName = speaker,
            dialogText = text,
            displayTime = duration
        };
        
        List<DialogLine> singleLine = new List<DialogLine> { line };
        ShowDialog(singleLine);
    }
}
