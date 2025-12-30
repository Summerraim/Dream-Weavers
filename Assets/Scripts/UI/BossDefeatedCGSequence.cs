using System.Collections;
using UnityEngine;

/// <summary>
/// Boss击败后的特殊CG动画序列控制器
/// 用于管理多个GameObject的动画播放顺序
/// </summary>
public class BossDefeatedCGSequence : MonoBehaviour
{
    [Header("CG动画物体")]
    [SerializeField]
    [Tooltip("开场动画物体")]
    private GameObject cgObject1;

    [SerializeField]
    [Tooltip("剧情CG动画物体1（左侧或指定位置）")]
    private GameObject cgObject2;

    [SerializeField]
    [Tooltip("剧情CG动画物体2（中央或指定位置）")]
    private GameObject cgObject3;

    [SerializeField]
    [Tooltip("剧情CG动画物体3（右侧或指定位置）")]
    private GameObject cgObject4;

    [SerializeField]
    [Tooltip("结尾动画物体")]
    private GameObject cgObject5;

    [Header("动画控制器")]
    [SerializeField]
    [Tooltip("开场动画的Animator")]
    private Animator animator1;

    [SerializeField]
    [Tooltip("剧情CG动画1的Animator")]
    private Animator animator2;

    [SerializeField]
    [Tooltip("剧情CG动画2的Animator")]
    private Animator animator3;

    [SerializeField]
    [Tooltip("剧情CG动画3的Animator")]
    private Animator animator4;

    [SerializeField]
    [Tooltip("结尾动画的Animator")]
    private Animator animator5;

    [Header("动画剪辑名称")]
    [SerializeField]
    [Tooltip("开场动画的Animation Clip名称")]
    private string animation1Name = "Opening";

    [SerializeField]
    [Tooltip("剧情CG动画的Animation Clip名称（如果三个动画使用相同名称）")]
    private string animation234Name = "StorySequence";

    [SerializeField]
    [Tooltip("结尾动画的Animation Clip名称")]
    private string animation5Name = "Ending";

    [Header("CG容器")]
    [SerializeField]
    [Tooltip("整个CG序列的容器Panel，初始应设为隐藏")]
    private GameObject cgPanel;

    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否在开始时自动播放（仅用于测试）")]
    private bool autoPlayOnStart = false;

    // 回调事件：当整个CG序列播放完成时触发
    public System.Action OnSequenceComplete;

    // 是否正在播放
    private bool isPlaying = false;

    private void Start()
    {
        // 确保所有CG物体初始状态为隐藏
        InitializeCGObjects();

        // 如果启用了自动播放（仅用于测试）
        if (autoPlayOnStart)
        {
            PlaySequence();
        }
    }

    /// <summary>
    /// 初始化所有CG物体为隐藏状态
    /// </summary>
    private void InitializeCGObjects()
    {
        if (cgObject1 != null) cgObject1.SetActive(false);
        if (cgObject2 != null) cgObject2.SetActive(false);
        if (cgObject3 != null) cgObject3.SetActive(false);
        if (cgObject4 != null) cgObject4.SetActive(false);
        if (cgObject5 != null) cgObject5.SetActive(false);

        if (cgPanel != null)
        {
            cgPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 开始播放CG序列
    /// </summary>
    public void PlaySequence()
    {
        if (isPlaying)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] 已经在播放CG，跳过重复调用");
            return;
        }

        Debug.Log("[BossDefeatedCGSequence] 开始播放Boss击败CG序列");
        StartCoroutine(PlayCGSequenceCoroutine());
    }

    /// <summary>
    /// CG序列播放协程
    /// </summary>
    private IEnumerator PlayCGSequenceCoroutine()
    {
        isPlaying = true;

        // 显示CG容器Panel
        if (cgPanel != null)
        {
            cgPanel.SetActive(true);
            Debug.Log("[BossDefeatedCGSequence] 显示CG Panel");
        }

        // ===== 阶段1: 播放开场动画 =====
        Debug.Log("[BossDefeatedCGSequence] 阶段1：播放开场动画");

        if (cgObject1 != null && animator1 != null)
        {
            cgObject1.SetActive(true);
            animator1.Play(animation1Name, 0, 0f); // 从头开始播放

            // 等待动画1播放完成
            yield return StartCoroutine(WaitForAnimationComplete(animator1, animation1Name));

            cgObject1.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] 开场动画播放完成");
        }
        else
        {
            Debug.LogWarning("[BossDefeatedCGSequence] cgObject1 或 animator1 未设置，跳过开场动画");
        }

        // ===== 阶段2: 同时播放剧情CG（物体2、3、4）=====
        Debug.Log("[BossDefeatedCGSequence] 阶段2：同时播放剧情CG（物体2、3、4）");

        // 激活并播放三个剧情CG物体
        if (cgObject2 != null && animator2 != null)
        {
            cgObject2.SetActive(true);
            animator2.Play(animation234Name, 0, 0f);
        }

        if (cgObject3 != null && animator3 != null)
        {
            cgObject3.SetActive(true);
            animator3.Play(animation234Name, 0, 0f);
        }

        if (cgObject4 != null && animator4 != null)
        {
            cgObject4.SetActive(true);
            animator4.Play(animation234Name, 0, 0f);
        }

        // 等待所有剧情动画播放完成（以animator2为基准，假设三个动画时长相同）
        if (animator2 != null)
        {
            yield return StartCoroutine(WaitForAnimationComplete(animator2, animation234Name));
            Debug.Log("[BossDefeatedCGSequence] 剧情CG播放完成");
        }

        // 隐藏剧情CG物体
        if (cgObject2 != null) cgObject2.SetActive(false);
        if (cgObject3 != null) cgObject3.SetActive(false);
        if (cgObject4 != null) cgObject4.SetActive(false);

        // ===== 阶段3: 播放结尾动画 =====
        Debug.Log("[BossDefeatedCGSequence] 阶段3：播放结尾动画");

        if (cgObject5 != null && animator5 != null)
        {
            cgObject5.SetActive(true);
            animator5.Play(animation5Name, 0, 0f);

            // 等待结尾动画播放完成
            yield return StartCoroutine(WaitForAnimationComplete(animator5, animation5Name));

            cgObject5.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] 结尾动画播放完成");
        }
        else
        {
            Debug.LogWarning("[BossDefeatedCGSequence] cgObject5 或 animator5 未设置，跳过结尾动画");
        }

        // 隐藏CG容器Panel
        if (cgPanel != null)
        {
            cgPanel.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] 隐藏CG Panel");
        }

        isPlaying = false;

        // 触发完成回调
        Debug.Log("[BossDefeatedCGSequence] CG序列播放完成，触发回调");
        OnSequenceComplete?.Invoke();
    }

    /// <summary>
    /// 等待指定动画播放完成的协程
    /// </summary>
    private IEnumerator WaitForAnimationComplete(Animator animator, string stateName)
    {
        if (animator == null)
        {
            Debug.LogWarning($"[BossDefeatedCGSequence] Animator为null，无法等待动画 {stateName}");
            yield break;
        }

        // 等待一帧，确保动画开始播放
        yield return null;

        // 获取当前动画状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 如果动画还没开始播放，再等待一帧
        if (!stateInfo.IsName(stateName))
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        // 持续检查动画是否播放完成
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        Debug.Log($"[BossDefeatedCGSequence] 动画 {stateName} 播放完成 (normalizedTime: {stateInfo.normalizedTime})");
    }

    /// <summary>
    /// 强制停止CG播放（可选功能，当前需求不需要）
    /// </summary>
    public void StopSequence()
    {
        if (isPlaying)
        {
            StopAllCoroutines();
            InitializeCGObjects();
            isPlaying = false;
            Debug.Log("[BossDefeatedCGSequence] CG序列已强制停止");
        }
    }

    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中验证配置
    /// </summary>
    private void OnValidate()
    {
        // 检查是否所有必需的字段都已设置
        if (cgPanel == null)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] CG Panel未设置，请在Inspector中配置");
        }

        if (cgObject1 == null || animator1 == null)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] 开场动画物体或Animator未设置");
        }

        if (cgObject5 == null || animator5 == null)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] 结尾动画物体或Animator未设置");
        }
    }
#endif
}
