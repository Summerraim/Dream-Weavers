using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话界面UI控制器
/// 负责管理对话界面的显示、动画和用户交互
/// </summary>
public class UI_DialogView : MonoBehaviour
{
    #region UI元素引用
    
    [Header("主界面")]
    [SerializeField] private GameObject dialogContainer;  // 对话容器
    [SerializeField] private Image background;           // 背景
    
    [Header("对话框面板")]
    [SerializeField] private GameObject dialogPanel;      // 对话框面板
    [SerializeField] private Image dialogBackground;     // 对话框背景
    [SerializeField] private TextMeshProUGUI dialogText; // 对话文本
    [SerializeField] private TextMeshProUGUI speakerNameText; // 说话者名字
    
    [Header("角色立绘")]
    [SerializeField] private Image leftCharacterPortrait;    // 左侧角色立绘
    [SerializeField] private Image rightCharacterPortrait;   // 右侧角色立绘
    [SerializeField] private Transform portraitContainer;    // 立绘容器
    
    [Header("对话控制")]
    [SerializeField] private GameObject continueIndicator;   // 继续指示箭头
    [SerializeField] private Button continueButton;          // 继续按钮
    [SerializeField] private Button skipButton;              // 跳过按钮
    [SerializeField] private Button autoButton;              // 自动播放按钮
    [SerializeField] private Button logButton;               // 对话日志按钮
    
    [Header("选项面板")]
    [SerializeField] private GameObject choicePanel;         // 选项面板
    [SerializeField] private Transform choiceContainer;      // 选项容器
    [SerializeField] private GameObject choiceButtonPrefab;  // 选项按钮预制体
    
    [Header("动画设置")]
    [SerializeField] private float textSpeed = 0.05f;        // 打字机效果速度
    [SerializeField] private float fadeDuration = 0.3f;      // 淡入淡出时间
    [SerializeField] private float indicatorBlinkSpeed = 1f; // 指示箭头闪烁速度
    
    [Header("音效")]
    [SerializeField] private AudioClip textTypingSound;      // 打字音效
    [SerializeField] private AudioClip optionSelectSound;    // 选项选择音效
    [SerializeField] private AudioClip dialogOpenSound;      // 对话打开音效
    [SerializeField] private AudioClip dialogCloseSound;     // 对话关闭音效
    
    #endregion
    
    #region 私有变量
    
    private Coroutine typingCoroutine;           // 打字机效果协程
    private Coroutine indicatorBlinkCoroutine;   // 箭头闪烁协程
    private bool isTyping = false;               // 是否正在显示文本
    private bool isAutoMode = false;             // 是否自动模式
    private bool isSkipping = false;             // 是否正在跳过
    private float currentTextSpeed;              // 当前文本速度
    
    private DialogController dialogController;   // 对话控制器引用
    private CanvasGroup containerCanvasGroup;    // 容器CanvasGroup
    private RectTransform dialogPanelRect;       // 对话框面板RectTransform
    
    // 立绘缓存
    private CharacterPortrait leftCharacter;
    private CharacterPortrait rightCharacter;
    
    #endregion
    
    #region 事件
    
    public event Action OnDialogContinue;        // 继续对话事件
    public event Action<int> OnChoiceSelected;   // 选项选择事件
    public event Action OnDialogSkip;            // 跳过对话事件
    public event Action OnDialogAutoToggle;      // 自动播放切换事件
    
    #endregion
    
    #region Unity生命周期
    
    private void Awake()
    {
        InitializeComponents();
        SetupEventListeners();
        
        // 默认隐藏对话界面
        HideDialogUI();
    }
    
    private void Start()
    {
        // 获取对话控制器引用
        dialogController = FindObjectOfType<DialogController>();
        if (dialogController == null)
        {
            Debug.LogWarning("未找到DialogController，对话系统可能无法正常工作");
        }
    }
    
    private void Update()
    {
        // 处理快捷键
        HandleHotkeys();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 确保有CanvasGroup用于淡入淡出
        if (dialogContainer != null && dialogContainer.GetComponent<CanvasGroup>() == null)
        {
            containerCanvasGroup = dialogContainer.AddComponent<CanvasGroup>();
        }
        else if (dialogContainer != null)
        {
            containerCanvasGroup = dialogContainer.GetComponent<CanvasGroup>();
        }
        
        // 获取对话框面板的RectTransform
        if (dialogPanel != null)
        {
            dialogPanelRect = dialogPanel.GetComponent<RectTransform>();
        }
        
        // 初始化立绘
        InitializePortraits();
    }
    
    /// <summary>
    /// 初始化立绘
    /// </summary>
    private void InitializePortraits()
    {
        // 创建左侧角色立绘对象
        if (leftCharacterPortrait != null)
        {
            leftCharacter = new CharacterPortrait(leftCharacterPortrait);
        }
        
        // 创建右侧角色立绘对象
        if (rightCharacterPortrait != null)
        {
            rightCharacter = new CharacterPortrait(rightCharacterPortrait);
        }
    }
    
    /// <summary>
    /// 设置事件监听
    /// </summary>
    private void SetupEventListeners()
    {
        // 继续按钮
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        // 跳过按钮
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
        
        // 自动播放按钮
        if (autoButton != null)
        {
            autoButton.onClick.AddListener(OnAutoButtonClicked);
        }
        
        // 对话日志按钮
        if (logButton != null)
        {
            logButton.onClick.AddListener(OnLogButtonClicked);
        }
        
        // 整个对话框面板点击
        if (dialogPanel != null)
        {
            Button panelButton = dialogPanel.GetComponent<Button>();
            if (panelButton == null)
            {
                panelButton = dialogPanel.AddComponent<Button>();
                panelButton.transition = Selectable.Transition.None;
            }
            panelButton.onClick.AddListener(OnDialogPanelClicked);
        }
    }
    
    #endregion
    
    #region UI显示控制
    
    /// <summary>
    /// 显示对话界面
    /// </summary>
    public void ShowDialogUI()
    {
        if (dialogContainer != null)
        {
            dialogContainer.SetActive(true);
            StartCoroutine(FadeInUI());
            
            // 播放打开音效
            PlayDialogOpenSound();
        }
    }
    
    /// <summary>
    /// 隐藏对话界面
    /// </summary>
    public void HideDialogUI()
    {
        if (dialogContainer != null)
        {
            StartCoroutine(FadeOutUI(() => {
                dialogContainer.SetActive(false);
                
                // 播放关闭音效
                PlayDialogCloseSound();
            }));
        }
    }
    
    /// <summary>
    /// 淡入UI
    /// </summary>
    private IEnumerator FadeInUI()
    {
        if (containerCanvasGroup != null)
        {
            float timer = 0f;
            containerCanvasGroup.alpha = 0f;
            
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                containerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            
            containerCanvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// 淡出UI
    /// </summary>
    private IEnumerator FadeOutUI(Action onComplete = null)
    {
        if (containerCanvasGroup != null)
        {
            float timer = 0f;
            containerCanvasGroup.alpha = 1f;
            
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                containerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            
            containerCanvasGroup.alpha = 0f;
        }
        
        onComplete?.Invoke();
    }
    
    #endregion
    
    #region 对话文本显示
    
    /// <summary>
    /// 显示对话
    /// </summary>
    public void ShowDialogue(string speakerName, string dialogueText, Sprite portrait = null, 
                            PortraitPosition portraitPosition = PortraitPosition.Left, 
                            bool isImportant = false)
    {
        // 设置说话者名字
        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }
        
        // 设置角色立绘
        SetCharacterPortrait(portrait, portraitPosition);
        
        // 高亮重要对话
        if (isImportant && dialogBackground != null)
        {
            dialogBackground.color = new Color(1f, 1f, 0.8f, 0.95f);
        }
        else if (dialogBackground != null)
        {
            dialogBackground.color = new Color(1f, 1f, 1f, 0.95f);
        }
        
        // 显示对话文本（打字机效果）
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        typingCoroutine = StartCoroutine(TypeText(dialogueText));
    }
    
    /// <summary>
    /// 打字机效果显示文本
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        
        if (dialogText != null)
        {
            dialogText.text = "";
            
            // 隐藏继续指示器
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(false);
            }
            
            // 播放打字音效
            AudioSource audioSource = GetComponent<AudioSource>();
            
            for (int i = 0; i < text.Length; i++)
            {
                // 如果正在跳过，立即显示全部文本
                if (isSkipping)
                {
                    dialogText.text = text;
                    break;
                }
                
                // 添加字符
                dialogText.text += text[i];
                
                // 播放打字音效（每个字符都播放可能会太吵，可以调整频率）
                if (audioSource != null && textTypingSound != null && i % 2 == 0)
                {
                    audioSource.PlayOneShot(textTypingSound, 0.3f);
                }
                
                // 等待
                if (!isAutoMode)
                {
                    yield return new WaitForSeconds(currentTextSpeed);
                }
                else
                {
                    // 自动模式下根据文本长度调整速度
                    float autoSpeed = Mathf.Min(currentTextSpeed * 2, 0.1f);
                    yield return new WaitForSeconds(autoSpeed);
                }
            }
        }
        
        isTyping = false;
        
        // 显示继续指示器
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
            
            // 开始闪烁动画
            if (indicatorBlinkCoroutine != null)
            {
                StopCoroutine(indicatorBlinkCoroutine);
            }
            indicatorBlinkCoroutine = StartCoroutine(BlinkContinueIndicator());
        }
        
        // 如果是自动模式，等待一段时间后自动继续
        if (isAutoMode)
        {
            yield return new WaitForSeconds(2f); // 等待2秒后自动继续
            OnDialogContinue?.Invoke();
        }
    }
    
    /// <summary>
    /// 立即完成当前文本显示
    /// </summary>
    public void CompleteTextDisplay()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
            
            // 显示完整文本
            if (dialogText != null)
            {
                // 这里需要获取完整的文本，可能需要从DialogController获取
                // 暂时留空，由外部调用时传递完整文本
            }
            
            // 显示继续指示器
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
                if (indicatorBlinkCoroutine != null)
                {
                    StopCoroutine(indicatorBlinkCoroutine);
                }
                indicatorBlinkCoroutine = StartCoroutine(BlinkContinueIndicator());
            }
        }
    }
    
    /// <summary>
    /// 闪烁继续指示器
    /// </summary>
    private IEnumerator BlinkContinueIndicator()
    {
        if (continueIndicator == null) yield break;
        
        CanvasGroup indicatorCanvasGroup = continueIndicator.GetComponent<CanvasGroup>();
        if (indicatorCanvasGroup == null)
        {
            indicatorCanvasGroup = continueIndicator.AddComponent<CanvasGroup>();
        }
        
        while (true)
        {
            // 淡入
            float timer = 0f;
            while (timer < indicatorBlinkSpeed)
            {
                timer += Time.deltaTime;
                indicatorCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / indicatorBlinkSpeed);
                yield return null;
            }
            
            // 淡出
            timer = 0f;
            while (timer < indicatorBlinkSpeed)
            {
                timer += Time.deltaTime;
                indicatorCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / indicatorBlinkSpeed);
                yield return null;
            }
        }
    }
    
    #endregion
    
    #region 角色立绘管理
    
    /// <summary>
    /// 设置角色立绘
    /// </summary>
    private void SetCharacterPortrait(Sprite portrait, PortraitPosition position)
    {
        // 重置所有立绘高亮
        ResetAllPortraitHighlights();
        
        // 设置立绘
        switch (position)
        {
            case PortraitPosition.Left:
                if (leftCharacter != null)
                {
                    leftCharacter.SetPortrait(portrait);
                    leftCharacter.Highlight(true);
                }
                if (rightCharacter != null)
                {
                    rightCharacter.Highlight(false);
                }
                break;
                
            case PortraitPosition.Right:
                if (rightCharacter != null)
                {
                    rightCharacter.SetPortrait(portrait);
                    rightCharacter.Highlight(true);
                }
                if (leftCharacter != null)
                {
                    leftCharacter.Highlight(false);
                }
                break;
                
            case PortraitPosition.None:
                if (leftCharacter != null) leftCharacter.Highlight(false);
                if (rightCharacter != null) rightCharacter.Highlight(false);
                break;
                
            case PortraitPosition.Center:
                // 中心位置可能需要特殊处理
                if (leftCharacter != null) leftCharacter.Highlight(false);
                if (rightCharacter != null) rightCharacter.Highlight(false);
                break;
        }
    }
    
    /// <summary>
    /// 重置所有立绘高亮
    /// </summary>
    private void ResetAllPortraitHighlights()
    {
        if (leftCharacter != null) leftCharacter.Highlight(false);
        if (rightCharacter != null) rightCharacter.Highlight(false);
    }
    
    /// <summary>
    /// 显示角色立绘动画
    /// </summary>
    public void ShowCharacterPortraitAnimation(string characterId, string animationName)
    {
        // 这里可以实现立绘动画（如表情变化、入场动画等）
        // 需要根据具体需求实现
    }
    
    #endregion
    
    #region 选项系统
    
    /// <summary>
    /// 显示选项
    /// </summary>
    public void ShowChoices(string[] choices)
    {
        if (choicePanel == null || choiceContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("选项系统未正确配置");
            return;
        }
        
        // 清除现有选项
        ClearChoices();
        
        // 显示选项面板
        choicePanel.SetActive(true);
        
        // 创建选项按钮
        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i; // 捕获循环变量
            
            GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            Button button = choiceButton.GetComponent<Button>();
            
            if (buttonText != null)
            {
                buttonText.text = choices[i];
            }
            
            if (button != null)
            {
                button.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
            }
        }
        
        // 隐藏继续指示器
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// 清除选项
    /// </summary>
    public void ClearChoices()
    {
        if (choiceContainer != null)
        {
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 选项按钮点击事件
    /// </summary>
    private void OnChoiceButtonClicked(int choiceIndex)
    {
        // 播放选择音效
        PlayOptionSelectSound();
        
        // 触发选项选择事件
        OnChoiceSelected?.Invoke(choiceIndex);
        
        // 隐藏选项面板
        ClearChoices();
    }
    
    #endregion
    
    #region 按钮事件处理
    
    /// <summary>
    /// 继续按钮点击
    /// </summary>
    private void OnContinueButtonClicked()
    {
        // 如果正在显示文本，立即完成显示
        if (isTyping)
        {
            CompleteTextDisplay();
        }
        else
        {
            // 触发继续对话事件
            OnDialogContinue?.Invoke();
        }
    }
    
    /// <summary>
    /// 跳过按钮点击
    /// </summary>
    private void OnSkipButtonClicked()
    {
        // 触发跳过对话事件
        OnDialogSkip?.Invoke();
    }
    
    /// <summary>
    /// 自动播放按钮点击
    /// </summary>
    private void OnAutoButtonClicked()
    {
        isAutoMode = !isAutoMode;
        
        // 更新按钮状态
        if (autoButton != null)
        {
            Image buttonImage = autoButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isAutoMode ? new Color(0.5f, 1f, 0.5f) : Color.white;
            }
        }
        
        // 触发自动播放切换事件
        OnDialogAutoToggle?.Invoke();
    }
    
    /// <summary>
    /// 对话日志按钮点击
    /// </summary>
    private void OnLogButtonClicked()
    {
        // 显示对话日志
        // 需要实现对话日志系统
        Debug.Log("打开对话日志");
    }
    
    /// <summary>
    /// 对话框面板点击
    /// </summary>
    private void OnDialogPanelClicked()
    {
        // 如果正在显示文本，立即完成显示
        if (isTyping)
        {
            CompleteTextDisplay();
        }
        else if (!choicePanel.activeSelf) // 如果没有显示选项
        {
            // 触发继续对话事件
            OnDialogContinue?.Invoke();
        }
    }
    
    #endregion
    
    #region 快捷键处理
    
    /// <summary>
    /// 处理快捷键
    /// </summary>
    private void HandleHotkeys()
    {
        if (!dialogContainer.activeSelf) return;
        
        // 空格键或回车键继续对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnContinueButtonClicked();
        }
        
        // Ctrl键跳过
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            OnSkipButtonClicked();
        }
        
        // A键切换自动播放
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnAutoButtonClicked();
        }
        
        // Esc键隐藏对话框（可能需要确认）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 可以添加确认对话框
            // HideDialogUI();
        }
    }
    
    #endregion
    
    #region 音效播放
    
    /// <summary>
    /// 播放对话打开音效
    /// </summary>
    private void PlayDialogOpenSound()
    {
        if (dialogOpenSound != null && AudioManagerService.Instance != null)
        {
            AudioManagerService.Instance.PlayUISFX(dialogOpenSound);
        }
    }
    
    /// <summary>
    /// 播放对话关闭音效
    /// </summary>
    private void PlayDialogCloseSound()
    {
        if (dialogCloseSound != null && AudioManagerService.Instance != null)
        {
            AudioManagerService.Instance.PlayUISFX(dialogCloseSound);
        }
    }
    
    /// <summary>
    /// 播放选项选择音效
    /// </summary>
    private void PlayOptionSelectSound()
    {
        if (optionSelectSound != null && AudioManagerService.Instance != null)
        {
            AudioManagerService.Instance.PlayUISFX(optionSelectSound);
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置文本显示速度
    /// </summary>
    public void SetTextSpeed(float speed)
    {
        currentTextSpeed = Mathf.Clamp(speed, 0.01f, 0.1f);
    }
    
    /// <summary>
    /// 获取当前是否正在显示文本
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }
    
    /// <summary>
    /// 获取当前是否为自动模式
    /// </summary>
    public bool IsAutoMode()
    {
        return isAutoMode;
    }
    
    /// <summary>
    /// 设置跳过状态
    /// </summary>
    public void SetSkipState(bool skipping)
    {
        isSkipping = skipping;
    }
    
    /// <summary>
    /// 清空对话文本
    /// </summary>
    public void ClearDialogueText()
    {
        if (dialogText != null)
        {
            dialogText.text = "";
        }
        
        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }
    }
    
    #endregion
    
    #region 辅助类
    
    /// <summary>
    /// 立绘位置枚举
    /// </summary>
    public enum PortraitPosition
    {
        Left,
        Right,
        Center,
        None
    }
    
    /// <summary>
    /// 角色立绘类
    /// </summary>
    private class CharacterPortrait
    {
        private Image portraitImage;
        private CanvasGroup canvasGroup;
        private Color originalColor;
        
        public CharacterPortrait(Image image)
        {
            portraitImage = image;
            canvasGroup = image.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            }
            
            originalColor = image.color;
            
            // 默认隐藏
            image.gameObject.SetActive(false);
        }
        
        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage != null)
            {
                if (sprite != null)
                {
                    portraitImage.sprite = sprite;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }
        }
        
        public void Highlight(bool highlight)
        {
            if (portraitImage != null)
            {
                if (highlight)
                {
                    portraitImage.color = Color.white;
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }
                else
                {
                    portraitImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                    if (canvasGroup != null) canvasGroup.alpha = 0.7f;
                }
            }
        }
    }
    
    #endregion
}
