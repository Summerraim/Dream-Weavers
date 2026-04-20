using System;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    [Header("Dialog UI")]
    [SerializeField]
    private UI_DialogView dialogView;

    [Header("Settings")]
    [SerializeField]
    private DialogueData customDialogueData;

    [SerializeField]
    private bool enableDebugLogs = true;

    [Header("Runtime")]
    [SerializeField]
    private DialogueData currentDialogue;

    [SerializeField]
    private int currentEntryIndex = -1;

    public event Action OnDialogueEnd;

    private bool isDialogueActive;

    private void Start()
    {
        EnsureDialogView();
        SubscribeDialogViewEvents();

        if (customDialogueData != null)
        {
            StartDialogue(customDialogueData);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeDialogViewEvents();
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        LogDebug(
            $"StartDialogue: {(dialogueData != null ? dialogueData.dialogueId : "null")}"
        );

        if (dialogueData == null || dialogueData.dialogueEntries == null || dialogueData.dialogueEntries.Length == 0)
        {
            LogDebug("Dialogue data is empty");
            return;
        }

        if (!EnsureDialogView())
        {
            LogDebug("Dialog view not found");
            return;
        }

        if (!CheckUIComponents())
        {
            LogDebug("Dialog UI is incomplete");
            return;
        }

        if (isDialogueActive)
        {
            EndDialogue();
            StartCoroutine(StartDialogueAfterEnd(dialogueData));
            return;
        }

        currentDialogue = dialogueData;
        currentEntryIndex = -1;
        isDialogueActive = true;

        dialogView.ShowDialogUI();
        dialogView.ClearDialogueText();
        ShowNextDialogueEntry();
    }

    public void EndDialogue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (dialogView != null)
        {
            dialogView.HideDialogUI();
        }

        DialogueData completedDialogue = currentDialogue;
        currentDialogue = null;
        currentEntryIndex = -1;
        isDialogueActive = false;

        OnDialogueEnd?.Invoke();

        if (completedDialogue != null)
        {
            LogDebug($"Dialogue ended: {completedDialogue.dialogueId}");
        }
        else
        {
            LogDebug("Dialogue ended");
        }
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    public DialogueData GetCurrentDialogue()
    {
        return currentDialogue;
    }

    public DialogueEntry GetCurrentDialogueEntry()
    {
        if (currentDialogue == null)
        {
            return null;
        }

        if (currentEntryIndex < 0 || currentEntryIndex >= currentDialogue.dialogueEntries.Length)
        {
            return null;
        }

        return currentDialogue.dialogueEntries[currentEntryIndex];
    }

    private System.Collections.IEnumerator StartDialogueAfterEnd(DialogueData dialogueData)
    {
        yield return null;
        StartDialogue(dialogueData);
    }

    private bool EnsureDialogView()
    {
        if (dialogView == null)
        {
            dialogView = FindObjectOfType<UI_DialogView>();
        }

        if (dialogView == null)
        {
            return false;
        }

        SubscribeDialogViewEvents();
        return true;
    }

    private void SubscribeDialogViewEvents()
    {
        if (dialogView == null)
        {
            return;
        }

        dialogView.OnDialogContinue -= OnContinueDialogue;
        dialogView.OnDialogSkip -= OnSkipDialogue;
        dialogView.OnDialogContinue += OnContinueDialogue;
        dialogView.OnDialogSkip += OnSkipDialogue;
    }

    private void UnsubscribeDialogViewEvents()
    {
        if (dialogView == null)
        {
            return;
        }

        dialogView.OnDialogContinue -= OnContinueDialogue;
        dialogView.OnDialogSkip -= OnSkipDialogue;
    }

    private bool CheckUIComponents()
    {
        return dialogView != null && dialogView.transform.childCount > 0;
    }

    private void OnContinueDialogue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (TryHandleSpecialContinue())
        {
            return;
        }

        ShowNextDialogueEntry();
    }

    private void OnSkipDialogue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        EndDialogue();
    }

    private void ShowNextDialogueEntry()
    {
        if (currentDialogue == null || currentDialogue.dialogueEntries == null)
        {
            return;
        }

        currentEntryIndex++;
        if (currentEntryIndex >= currentDialogue.dialogueEntries.Length)
        {
            EndDialogue();
            return;
        }

        DialogueEntry currentEntry = currentDialogue.dialogueEntries[currentEntryIndex];
        if (dialogView == null)
        {
            return;
        }

        try
        {
            dialogView.ShowDialogue(
                currentEntry.speakerName ?? "未知",
                currentEntry.dialogueText ?? string.Empty,
                currentEntry.portrait,
                currentEntry.portraitPosition
            );
        }
        catch (Exception e)
        {
            LogDebug($"Show dialogue failed: {e.Message}");
        }
    }

    private bool TryHandleSpecialContinue()
    {
        DialogueEntry currentEntry = GetCurrentDialogueEntry();
        if (currentEntry == null || !currentEntry.openSpiritPanelOnContinue)
        {
            return false;
        }

        SpiritPanelController spiritPanelController = FindObjectOfType<SpiritPanelController>(true);
        EndDialogue();

        if (spiritPanelController != null)
        {
            spiritPanelController.ShowPanel();
        }

        return true;
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DialogController] {message}");
        }
    }
}
