using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialogView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject dialogContainer;

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Image leftPortraitImage;

    [SerializeField]
    private Image rightPortraitImage;

    [SerializeField]
    private TextMeshProUGUI leftSpeakerNameText;

    [SerializeField]
    private TextMeshProUGUI rightSpeakerNameText;

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private Button continueButton;

    [SerializeField]
    private Button autoPlayButton;

    [SerializeField]
    private Button skipButton;

    [SerializeField]
    private TextMeshProUGUI autoPlayButtonText;

    [SerializeField]
    private float typingIntervalSeconds = 0.05f;

    [SerializeField]
    private float autoPlayDelaySeconds = 1f;

    public event Action OnDialogContinue;
    public event Action OnDialogSkip;

    private bool isTyping;
    private bool isAutoPlayEnabled;
    private string currentDialogueText;
    private Coroutine typingCoroutine;
    private Coroutine autoPlayCoroutine;

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (autoPlayButton != null)
        {
            autoPlayButton.onClick.AddListener(ToggleAutoPlay);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        RefreshAutoPlayButtonState();
    }

    private void OnEnable()
    {
        if (dialogContainer == null)
        {
            dialogContainer = GameObject.Find("DialogContainer") ?? GameObject.Find("DialogUI");
        }
    }

    private void OnDisable()
    {
        StopTypingCoroutine();
        StopAutoPlayCoroutine();
        isTyping = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnContinueButtonClicked();
        }
    }

    public void ShowDialogUI()
    {
        if (dialogContainer == null)
        {
            dialogContainer = GameObject.Find("DialogContainer") ?? GameObject.Find("DialogUI");
        }

        if (dialogContainer == null)
        {
            Debug.LogError("UI_DialogView: dialogContainer is null");
            return;
        }

        Canvas canvas = dialogContainer.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.sortingOrder = 1000;
        }

        dialogContainer.SetActive(true);
        RefreshAutoPlayButtonState();
    }

    public void HideDialogUI()
    {
        StopTypingCoroutine();
        StopAutoPlayCoroutine();
        isTyping = false;

        if (dialogContainer == null)
        {
            Debug.LogError("UI_DialogView: dialogContainer is null");
            return;
        }

        Canvas canvas = dialogContainer.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
        }

        dialogContainer.SetActive(false);
    }

    public void ShowDialogue(
        string speakerName,
        string dialogue,
        Sprite portrait,
        PortraitPosition position
    )
    {
        if (dialogContainer == null || !dialogContainer.activeInHierarchy)
        {
            ShowDialogUI();
        }

        UpdateSpeakerName(speakerName, position);
        SetPortrait(portrait, position);

        if (dialogueText == null)
        {
            dialogueText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (dialogueText == null)
        {
            Debug.LogError("UI_DialogView: dialogueText is null");
            return;
        }

        currentDialogueText = dialogue ?? string.Empty;
        dialogueText.enabled = true;
        StartTypingAnimation();

        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.gameObject.SetActive(true);
        }

        if (autoPlayButton != null)
        {
            autoPlayButton.gameObject.SetActive(true);
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
        }

        RefreshAutoPlayButtonState();
    }

    public void ClearDialogueText()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }
    }

    public void ToggleAutoPlay()
    {
        isAutoPlayEnabled = !isAutoPlayEnabled;
        RefreshAutoPlayButtonState();

        if (isAutoPlayEnabled && !isTyping && !string.IsNullOrEmpty(currentDialogueText))
        {
            TryScheduleAutoPlay();
        }
        else if (!isAutoPlayEnabled)
        {
            StopAutoPlayCoroutine();
        }
    }

    public bool IsAutoPlayEnabled()
    {
        return isAutoPlayEnabled;
    }

    public void SkipDialogue()
    {
        OnSkipButtonClicked();
    }

    private void UpdateSpeakerName(string speakerName, PortraitPosition position)
    {
        string displayName = string.IsNullOrEmpty(speakerName) ? "系统" : speakerName;

        if (position == PortraitPosition.Left)
        {
            if (leftSpeakerNameText != null)
            {
                leftSpeakerNameText.text = displayName;
                leftSpeakerNameText.enabled = true;
            }

            if (rightSpeakerNameText != null)
            {
                rightSpeakerNameText.enabled = false;
            }
            return;
        }

        if (position == PortraitPosition.Right)
        {
            if (rightSpeakerNameText != null)
            {
                rightSpeakerNameText.text = displayName;
                rightSpeakerNameText.enabled = true;
            }

            if (leftSpeakerNameText != null)
            {
                leftSpeakerNameText.enabled = false;
            }
            return;
        }

        if (leftSpeakerNameText != null)
        {
            leftSpeakerNameText.enabled = false;
        }

        if (rightSpeakerNameText != null)
        {
            rightSpeakerNameText.enabled = false;
        }
    }

    private void SetPortrait(Sprite portrait, PortraitPosition position)
    {
        if (leftPortraitImage != null)
        {
            leftPortraitImage.gameObject.SetActive(false);
        }

        if (rightPortraitImage != null)
        {
            rightPortraitImage.gameObject.SetActive(false);
        }

        if (portrait == null)
        {
            return;
        }

        if (position == PortraitPosition.Left && leftPortraitImage != null)
        {
            leftPortraitImage.sprite = portrait;
            leftPortraitImage.gameObject.SetActive(true);
        }
        else if (position == PortraitPosition.Right && rightPortraitImage != null)
        {
            rightPortraitImage.sprite = portrait;
            rightPortraitImage.gameObject.SetActive(true);
        }
    }

    private void StartTypingAnimation()
    {
        StopTypingCoroutine();
        StopAutoPlayCoroutine();

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        if (!gameObject.activeInHierarchy)
        {
            if (dialogueText != null)
            {
                dialogueText.text = currentDialogueText;
            }
            isTyping = false;
            TryScheduleAutoPlay();
            return;
        }

        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        dialogueText.text = string.Empty;

        foreach (char letter in currentDialogueText)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingIntervalSeconds);
        }

        isTyping = false;
        typingCoroutine = null;
        TryScheduleAutoPlay();
    }

    private void CompleteTextDisplay()
    {
        StopTypingCoroutine();

        if (dialogueText != null)
        {
            dialogueText.text = currentDialogueText;
        }

        isTyping = false;
        TryScheduleAutoPlay();
    }

    private void TryScheduleAutoPlay()
    {
        StopAutoPlayCoroutine();

        if (!isAutoPlayEnabled || !isActiveAndEnabled || string.IsNullOrEmpty(currentDialogueText))
        {
            return;
        }

        autoPlayCoroutine = StartCoroutine(AutoPlayNextCoroutine());
    }

    private IEnumerator AutoPlayNextCoroutine()
    {
        yield return new WaitForSeconds(autoPlayDelaySeconds);
        autoPlayCoroutine = null;

        if (!isTyping)
        {
            OnDialogContinue?.Invoke();
        }
    }

    private void StopTypingCoroutine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    private void StopAutoPlayCoroutine()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    private void RefreshAutoPlayButtonState()
    {
        if (autoPlayButtonText == null && autoPlayButton != null)
        {
            autoPlayButtonText = autoPlayButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (autoPlayButtonText != null)
        {
            autoPlayButtonText.text = isAutoPlayEnabled ? "自动播放:开" : "自动播放:关";
        }
    }

    private void OnContinueButtonClicked()
    {
        StopAutoPlayCoroutine();

        if (isTyping)
        {
            CompleteTextDisplay();
            return;
        }

        OnDialogContinue?.Invoke();
    }

    private void OnSkipButtonClicked()
    {
        StopTypingCoroutine();
        StopAutoPlayCoroutine();
        isTyping = false;
        OnDialogSkip?.Invoke();
    }

    public enum PortraitPosition
    {
        Left,
        Right,
        None,
    }
}
