using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 简化的对话UI组件
/// 专注于显示对话者的头像和对话内容
/// </summary>
public class UI_DialogView : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private GameObject dialogContainer;           // 对话容器
    [SerializeField] private Image backgroundImage;                // 背景图
    [SerializeField] private Image leftPortraitImage;              // 左侧头像
    [SerializeField] private Image rightPortraitImage;             // 右侧头像
    [SerializeField] private TextMeshProUGUI leftSpeakerNameText;  // 左侧说话者名字
    [SerializeField] private TextMeshProUGUI rightSpeakerNameText; // 右侧说话者名字
    [SerializeField] private TextMeshProUGUI dialogueText;         // 对话文本
    [SerializeField] private Button continueButton;                // 继续按钮

    // 事件
    public event Action OnDialogContinue;                          // 继续对话事件

    private bool isTyping = false;                                 // 是否正在显示文本
    private string currentDialogueText;                            // 当前对话文本
    private Coroutine typingCoroutine;                             // 打字协程

    #region Unity生命周期

    private void Awake()
    {
        Debug.Log("UI_DialogView.Awake 被调用");

        // 设置继续按钮事件
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            Debug.Log("UI_DialogView: 继续按钮事件已设置");
        }
        else
        {
            Debug.LogWarning("UI_DialogView: continueButton引用为空");
        }
    }

    private void OnEnable()
    {
        Debug.Log("UI_DialogView.OnEnable 被调用");

        // 只在 dialogContainer 未设置时尝试查找，不自动显示
        if (dialogContainer == null)
        {
            Debug.LogWarning("UI_DialogView: dialogContainer引用为空，尝试查找对话容器");
            dialogContainer = GameObject.Find("DialogContainer") ?? GameObject.Find("DialogUI");
            if (dialogContainer != null)
            {
                Debug.Log("UI_DialogView: 成功找到对话容器");
            }
        }
    }

    private void OnDisable()
    {
        Debug.Log("UI_DialogView.OnDisable 被调用");

        // 停止打字动画协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 重置打字状态
        isTyping = false;

        // OnDisable 时不强制隐藏 DialogPanel
        // 让 HideDialogUI() 方法来负责隐藏
    }

    private void Update()
    {
        // 空格键或回车键继续对话
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnContinueButtonClicked();
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示对话界面
    /// </summary>
    public void ShowDialogUI()
    {
        Debug.Log("UI_DialogView.ShowDialogUI 被调用");

        if (dialogContainer != null)
        {
            // 确保Canvas和所有UI组件都正确设置
            Canvas canvas = dialogContainer.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 1000; // 确保在最上层显示
            }

            dialogContainer.SetActive(true);
            Debug.Log("UI_DialogView: 对话界面已显示");
        }
        else
        {
            Debug.LogError("UI_DialogView: dialogContainer引用为空，无法显示对话界面");
            // 尝试查找对话容器
            dialogContainer = GameObject.Find("DialogContainer") ?? GameObject.Find("DialogUI");
            if (dialogContainer != null)
            {
                Debug.Log("UI_DialogView: 成功找到对话容器，重新显示");
                ShowDialogUI();
            }
        }
    }

    /// <summary>
    /// 隐藏对话界面
    /// </summary>
    public void HideDialogUI()
    {
        Debug.Log("UI_DialogView.HideDialogUI 被调用");

        // 停止打字动画协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 重置打字状态
        isTyping = false;

        if (dialogContainer != null)
        {
            // 禁用Canvas组件
            Canvas canvas = dialogContainer.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            // 禁用游戏对象
            dialogContainer.SetActive(false);

            Debug.Log("UI_DialogView: 对话界面已隐藏");
        }
        else
        {
            Debug.LogError("UI_DialogView: dialogContainer引用为空，无法隐藏对话界面");
        }
    }

    /// <summary>
    /// 显示对话
    /// </summary>
    public void ShowDialogue(string speakerName, string dialogue, Sprite portrait, PortraitPosition position)
    {
        Debug.Log($"UI_DialogView.ShowDialogue 被调用 - 说话者: {speakerName}, 文本长度: {(dialogue != null ? dialogue.Length : 0)}");
        
        // 确保对话界面已显示
        if (dialogContainer == null || !dialogContainer.activeInHierarchy)
        {
            ShowDialogUI();
        }
        
        // 设置说话者名字（根据位置显示在左侧或右侧）
        switch (position)
        {
            case PortraitPosition.Left:
                if (leftSpeakerNameText != null)
                {
                    leftSpeakerNameText.text = speakerName ?? "系统";
                    leftSpeakerNameText.enabled = true;
                    Debug.Log($"UI_DialogView: 设置左侧说话者名字: {speakerName}");
                }
                // 隐藏右侧说话者名字
                if (rightSpeakerNameText != null)
                {
                    rightSpeakerNameText.enabled = false;
                }
                break;
            case PortraitPosition.Right:
                if (rightSpeakerNameText != null)
                {
                    rightSpeakerNameText.text = speakerName ?? "系统";
                    rightSpeakerNameText.enabled = true;
                    Debug.Log($"UI_DialogView: 设置右侧说话者名字: {speakerName}");
                }
                // 隐藏左侧说话者名字
                if (leftSpeakerNameText != null)
                {
                    leftSpeakerNameText.enabled = false;
                }
                break;
            case PortraitPosition.None:
                // 隐藏所有说话者名字
                if (leftSpeakerNameText != null) leftSpeakerNameText.enabled = false;
                if (rightSpeakerNameText != null) rightSpeakerNameText.enabled = false;
                break;
        }
        
        // 如果找不到对应的说话者文本组件，尝试查找备用方案
        if ((position == PortraitPosition.Left && leftSpeakerNameText == null) || 
            (position == PortraitPosition.Right && rightSpeakerNameText == null))
        {
            Debug.LogWarning("UI_DialogView: 说话者名字文本组件引用为空，尝试查找备用组件");
            TextMeshProUGUI fallbackText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (fallbackText != null)
            {
                fallbackText.text = speakerName ?? "系统";
                Debug.Log("UI_DialogView: 成功找到备用说话者文本组件");
            }
        }

        // 设置头像
        SetPortrait(portrait, position);

        // 开始显示文本
        if (dialogueText != null)
        {
            currentDialogueText = dialogue ?? "暂无对话内容";
            dialogueText.enabled = true;
            Debug.Log($"UI_DialogView: 开始显示文本，长度: {currentDialogueText.Length}");
            StartTypingAnimation();
        }
        else
        {
            Debug.LogError("UI_DialogView: dialogueText引用为空");
            // 尝试查找对话文本组件
            dialogueText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (dialogueText != null)
            {
                currentDialogueText = dialogue ?? "暂无对话内容";
                StartTypingAnimation();
                Debug.Log("UI_DialogView: 成功找到对话文本组件");
            }
        }
        
        // 确保继续按钮可用
        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 清空对话文本
    /// </summary>
    public void ClearDialogueText()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 设置头像
    /// </summary>
    private void SetPortrait(Sprite portrait, PortraitPosition position)
    {
        // 隐藏所有头像
        if (leftPortraitImage != null)
        {
            leftPortraitImage.gameObject.SetActive(false);
        }
        if (rightPortraitImage != null)
        {
            rightPortraitImage.gameObject.SetActive(false);
        }

        // 根据位置显示对应的头像
        switch (position)
        {
            case PortraitPosition.Left:
                if (leftPortraitImage != null && portrait != null)
                {
                    leftPortraitImage.sprite = portrait;
                    leftPortraitImage.gameObject.SetActive(true);
                }
                break;
            case PortraitPosition.Right:
                if (rightPortraitImage != null && portrait != null)
                {
                    rightPortraitImage.sprite = portrait;
                    rightPortraitImage.gameObject.SetActive(true);
                }
                break;
            case PortraitPosition.None:
                // 不显示头像
                break;
        }
    }

    /// <summary>
    /// 开始打字动画
    /// </summary>
    private void StartTypingAnimation()
    {
        Debug.Log("UI_DialogView: 开始打字动画");
        
        // 确保游戏对象处于激活状态
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("UI_DialogView: 游戏对象未激活，尝试激活");
            gameObject.SetActive(true);
        }
        
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 再次检查游戏对象状态
        if (gameObject.activeInHierarchy)
        {
            typingCoroutine = StartCoroutine(TypeText());
            Debug.Log("UI_DialogView: 打字动画协程已启动");
        }
        else
        {
            Debug.LogError("UI_DialogView: 游戏对象仍处于非激活状态，无法启动协程");
            // 直接显示完整文本作为备选方案
            if (dialogueText != null)
            {
                dialogueText.text = currentDialogueText;
                Debug.Log("UI_DialogView: 直接显示完整文本");
            }
        }
    }

    /// <summary>
    /// 打字动画协程
    /// </summary>
    private IEnumerator TypeText()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in currentDialogueText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(0.05f); // 固定速度
        }

        isTyping = false;
    }

    /// <summary>
    /// 立即完成文本显示
    /// </summary>
    private void CompleteTextDisplay()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        if (dialogueText != null)
        {
            dialogueText.text = currentDialogueText;
        }

        isTyping = false;
    }

    #endregion

    #region 按钮事件

    /// <summary>
    /// 继续按钮点击事件
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

    #endregion

    #region 枚举

    /// <summary>
    /// 头像位置
    /// </summary>
    public enum PortraitPosition
    {
        Left,       // 左侧
        Right,      // 右侧
        None        // 不显示
    }

    #endregion
}
