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
    [Tooltip("剧情CG动画2的Animation Clip名称")]
    private string animation2Name = "StorySequence";

    [SerializeField]
    [Tooltip("剧情CG动画3的Animation Clip名称")]
    private string animation3Name = "StorySequence";

    [SerializeField]
    [Tooltip("剧情CG动画4的Animation Clip名称")]
    private string animation4Name = "StorySequence";

    [SerializeField]
    [Tooltip("结尾动画的Animation Clip名称")]
    private string animation5Name = "Ending";

    [Header("CG容器")]
    [SerializeField]
    [Tooltip("整个CG序列的容器Panel，初始应设为隐藏")]
    private GameObject cgPanel;

    [SerializeField]
    [Tooltip("所有CG动画播放完成后要激活的Panel")]
    private GameObject panelToActivateAfterCG;

    [Header("调试选项")]
    [SerializeField]
    [Tooltip("是否在GameObject激活时自动播放（仅用于测试）")]
    private bool autoPlayOnEnable = false;

    [SerializeField]
    [Tooltip("自动播放延迟时间（秒），给Inspector一些反应时间")]
    private float autoPlayDelay = 0.5f;

    // 回调事件：当整个CG序列播放完成时触发
    public System.Action OnSequenceComplete;

    // 是否正在播放
    private bool isPlaying = false;

    private void Start()
    {
        // 确保所有CG物体初始状态为隐藏
        InitializeCGObjects();
    }

    private void OnEnable()
    {
        // 如果启用了自动播放（仅用于测试）
        if (autoPlayOnEnable && !isPlaying)
        {
            Debug.Log("[BossDefeatedCGSequence] Auto Play已启用，将在" + autoPlayDelay + "秒后开始播放");
            StartCoroutine(AutoPlayAfterDelay());
        }
    }

    /// <summary>
    /// 延迟自动播放（用于测试）
    /// </summary>
    private IEnumerator AutoPlayAfterDelay()
    {
        yield return new WaitForSeconds(autoPlayDelay);
        if (!isPlaying)
        {
            Debug.Log("[BossDefeatedCGSequence] 开始自动播放测试");
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

        // 确保完成后的Panel初始为隐藏
        if (panelToActivateAfterCG != null)
        {
            panelToActivateAfterCG.SetActive(false);
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

        // 验证配置
        if (!ValidateConfiguration())
        {
            Debug.LogError("[BossDefeatedCGSequence] 配置验证失败，无法播放CG。请检查Inspector配置！");
            return;
        }

        Debug.Log("[BossDefeatedCGSequence] 开始播放Boss击败CG序列");
        StartCoroutine(PlayCGSequenceCoroutine());
    }

    /// <summary>
    /// 验证配置是否正确
    /// </summary>
    private bool ValidateConfiguration()
    {
        bool isValid = true;

        if (cgPanel == null)
        {
            Debug.LogError("[BossDefeatedCGSequence] ❌ CG Panel 未设置！请在Inspector中配置 'Cg Panel' 字段");
            isValid = false;
        }
        else
        {
            Debug.Log($"[BossDefeatedCGSequence] ✅ CG Panel 已配置: {cgPanel.name}");
        }

        if (cgObject1 == null || animator1 == null)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] ⚠️ 开场动画（Object1/Animator1）未配置");
        }

        if (cgObject5 == null || animator5 == null)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] ⚠️ 结尾动画（Object5/Animator5）未配置");
        }

        int middleAnimCount = 0;
        if (cgObject2 != null && animator2 != null) middleAnimCount++;
        if (cgObject3 != null && animator3 != null) middleAnimCount++;
        if (cgObject4 != null && animator4 != null) middleAnimCount++;

        if (middleAnimCount == 0)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] ⚠️ 中间剧情CG（Object2/3/4）没有配置任何一个");
        }
        else
        {
            Debug.Log($"[BossDefeatedCGSequence] ✅ 配置了 {middleAnimCount} 个中间剧情CG");
        }

        return isValid;
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
            Debug.Log("[BossDefeatedCGSequence] ✅ 显示CG Panel");
        }

        // 等待一帧，确保Panel完全激活
        yield return null;

        // ===== 阶段1: 播放开场动画 =====
        Debug.Log("[BossDefeatedCGSequence] === 阶段1：播放开场动画 ===");

        if (cgObject1 != null && animator1 != null)
        {
            Debug.Log($"[BossDefeatedCGSequence] 尝试激活 cgObject1: {cgObject1.name}");
            cgObject1.SetActive(true);

            // 等待一帧确保GameObject完全激活
            yield return null;

            Debug.Log($"[BossDefeatedCGSequence] cgObject1 激活状态: {cgObject1.activeSelf}, activeInHierarchy: {cgObject1.activeInHierarchy}");
            Debug.Log($"[BossDefeatedCGSequence] 开始播放动画: {animation1Name}");

            animator1.Play(animation1Name, 0, 0f); // 从头开始播放

            // 等待一帧让动画开始
            yield return null;

            // 等待动画1播放完成
            Debug.Log($"[BossDefeatedCGSequence] 等待动画 {animation1Name} 播放完成...");
            yield return StartCoroutine(WaitForAnimationComplete(animator1, animation1Name));

            cgObject1.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] ✅ 开场动画播放完成");
        }
        else
        {
            Debug.LogWarning($"[BossDefeatedCGSequence] ⚠️ cgObject1({cgObject1?.name}) 或 animator1 未设置，跳过开场动画");
        }

        // ===== 阶段2: 同时播放剧情CG（物体2、3、4）=====
        Debug.Log("[BossDefeatedCGSequence] === 阶段2：同时播放剧情CG（物体2、3、4）===");

        int activeCount = 0;

        // 激活并播放三个剧情CG物体
        if (cgObject2 != null && animator2 != null)
        {
            Debug.Log($"[BossDefeatedCGSequence] 激活并播放 cgObject2: {cgObject2.name}，动画: {animation2Name}");
            cgObject2.SetActive(true);
            yield return null;
            animator2.Play(animation2Name, 0, 0f);
            activeCount++;
        }

        if (cgObject3 != null && animator3 != null)
        {
            Debug.Log($"[BossDefeatedCGSequence] 激活并播放 cgObject3: {cgObject3.name}，动画: {animation3Name}");
            cgObject3.SetActive(true);
            yield return null;
            animator3.Play(animation3Name, 0, 0f);
            activeCount++;
        }

        if (cgObject4 != null && animator4 != null)
        {
            Debug.Log($"[BossDefeatedCGSequence] 激活并播放 cgObject4: {cgObject4.name}，动画: {animation4Name}");
            cgObject4.SetActive(true);
            yield return null;
            animator4.Play(animation4Name, 0, 0f);
            activeCount++;
        }

        if (activeCount == 0)
        {
            Debug.LogWarning("[BossDefeatedCGSequence] ⚠️ 没有配置任何中间剧情CG，跳过阶段2");
        }
        else
        {
            Debug.Log($"[BossDefeatedCGSequence] 已启动 {activeCount} 个剧情CG动画，等待播放完成...");

            // 等待所有剧情动画播放完成（以第一个有效的animator为基准，假设三个动画时长相同）
            if (animator2 != null)
            {
                yield return StartCoroutine(WaitForAnimationComplete(animator2, animation2Name));
                Debug.Log("[BossDefeatedCGSequence] ✅ 剧情CG播放完成");
            }
            else if (animator3 != null)
            {
                yield return StartCoroutine(WaitForAnimationComplete(animator3, animation3Name));
            }
            else if (animator4 != null)
            {
                yield return StartCoroutine(WaitForAnimationComplete(animator4, animation4Name));
            }
        }

        // 隐藏剧情CG物体
        if (cgObject2 != null) cgObject2.SetActive(false);
        if (cgObject3 != null) cgObject3.SetActive(false);
        if (cgObject4 != null) cgObject4.SetActive(false);

        // ===== 阶段3: 播放结尾动画 =====
        Debug.Log("[BossDefeatedCGSequence] === 阶段3：播放结尾动画 ===");

        if (cgObject5 != null && animator5 != null)
        {
            Debug.Log($"[BossDefeatedCGSequence] 激活并播放 cgObject5: {cgObject5.name}");
            cgObject5.SetActive(true);
            yield return null;

            animator5.Play(animation5Name, 0, 0f);
            yield return null;

            // 等待结尾动画播放完成
            Debug.Log($"[BossDefeatedCGSequence] 等待结尾动画 {animation5Name} 播放完成...");
            yield return StartCoroutine(WaitForAnimationComplete(animator5, animation5Name));

            cgObject5.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] ✅ 结尾动画播放完成");
        }
        else
        {
            Debug.LogWarning($"[BossDefeatedCGSequence] ⚠️ cgObject5({cgObject5?.name}) 或 animator5 未设置，跳过结尾动画");
        }

        // 隐藏CG容器Panel
        if (cgPanel != null)
        {
            cgPanel.SetActive(false);
            Debug.Log("[BossDefeatedCGSequence] 隐藏CG Panel");
        }

        // 激活完成后的Panel
        if (panelToActivateAfterCG != null)
        {
            panelToActivateAfterCG.SetActive(true);
            Debug.Log("[BossDefeatedCGSequence] ✅ 激活完成后的Panel: " + panelToActivateAfterCG.name);
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

        // 等待两帧，确保动画系统初始化完成
        yield return null;
        yield return null;

        // 获取当前动画状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        Debug.Log($"[BossDefeatedCGSequence] 动画状态检查: IsName({stateName})={stateInfo.IsName(stateName)}, normalizedTime={stateInfo.normalizedTime:F3}");

        // 如果动画还没开始播放，等待最多1秒
        int waitFrames = 0;
        while (!stateInfo.IsName(stateName) && waitFrames < 60)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            waitFrames++;
        }

        if (!stateInfo.IsName(stateName))
        {
            Debug.LogError($"[BossDefeatedCGSequence] ❌ 动画 {stateName} 没有开始播放！可能的原因：\n" +
                          $"1. Animation Clip名称错误（应该是: '{stateName}'）\n" +
                          $"2. Animator Controller未正确配置\n" +
                          $"3. 当前状态: {stateInfo.fullPathHash}");

            // 作为fallback，等待2秒
            Debug.LogWarning($"[BossDefeatedCGSequence] 使用fallback：等待2秒");
            yield return new WaitForSeconds(2f);
            yield break;
        }

        Debug.Log($"[BossDefeatedCGSequence] ✅ 动画 {stateName} 已开始播放，等待完成...");

        // 持续检查动画是否播放完成
        int checkCount = 0;
        while (stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 每30帧打印一次进度（约0.5秒）
            if (checkCount % 30 == 0)
            {
                Debug.Log($"[BossDefeatedCGSequence] 动画播放进度: {stateInfo.normalizedTime * 100:F1}%");
            }
            checkCount++;

            // 超时保护（10秒）
            if (checkCount > 600)
            {
                Debug.LogWarning($"[BossDefeatedCGSequence] ⚠️ 动画播放超时（10秒），强制结束");
                break;
            }
        }

        Debug.Log($"[BossDefeatedCGSequence] ✅ 动画 {stateName} 播放完成 (normalizedTime: {stateInfo.normalizedTime:F3})");
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
