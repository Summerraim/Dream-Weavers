using UnityEngine;

/// <summary>
/// Effect图标诊断工具
/// 用于检查Effect的Image配置是否正确
/// </summary>
public class EffectIconDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    [SerializeField]
    private KeyCode diagnosticKey = KeyCode.F10; // 按F10运行诊断

    [Header("要检查的Effect资源")]
    [SerializeField]
    private Effect[] effectsToCheck; // 拖入你想检查的Effect资源

    private void Update()
    {
        if (Input.GetKeyDown(diagnosticKey))
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("运行Effect图标诊断")]
    public void RunDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("===== Effect图标诊断开始 =====");
        Debug.Log("========================================");

        if (effectsToCheck == null || effectsToCheck.Length == 0)
        {
            Debug.LogWarning("❌ 没有指定要检查的Effect！");
            Debug.LogWarning("   请在Inspector中将Effect资源拖入 'Effects To Check' 数组");

            // 尝试查找项目中所有的Effect
            Debug.Log("\n正在搜索项目中的所有Effect资源...");
            ScanAllEffectsInProject();
            return;
        }

        int totalEffects = effectsToCheck.Length;
        int effectsWithImage = 0;
        int effectsWithoutImage = 0;

        Debug.Log($"\n检查 {totalEffects} 个Effect资源:\n");

        for (int i = 0; i < effectsToCheck.Length; i++)
        {
            Effect effect = effectsToCheck[i];

            if (effect == null)
            {
                Debug.LogWarning($"[{i}] ⚠️ 数组元素为null");
                continue;
            }

            CheckEffect(effect, ref effectsWithImage, ref effectsWithoutImage);
        }

        // 总结
        Debug.Log("\n========================================");
        Debug.Log("===== 诊断结果总结 =====");
        Debug.Log($"总共检查: {totalEffects} 个Effect");
        Debug.Log($"✅ 有图标: {effectsWithImage} 个");
        Debug.Log($"❌ 无图标: {effectsWithoutImage} 个");

        if (effectsWithoutImage > 0)
        {
            Debug.LogWarning("\n⚠️ 发现问题！");
            Debug.LogWarning($"有 {effectsWithoutImage} 个Effect没有配置图标");
            Debug.LogWarning("解决方法：");
            Debug.LogWarning("1. 在Project窗口找到这些Effect资源");
            Debug.LogWarning("2. 在Inspector中找到 'Image' 字段");
            Debug.LogWarning("3. 从Project窗口拖入对应的Sprite图标");
        }
        else
        {
            Debug.Log("\n✅ 所有Effect都正确配置了图标！");
        }

        Debug.Log("========================================");
    }

    /// <summary>
    /// 检查单个Effect
    /// </summary>
    private void CheckEffect(Effect effect, ref int withImage, ref int withoutImage)
    {
        string effectName = effect.name;
        string displayName = effect.DisplayName;
        Sprite image = effect.Image;

        Debug.Log($"━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"Effect: {effectName}");
        Debug.Log($"  Display Name: {displayName}");

        if (image != null)
        {
            Debug.Log($"  ✅ Image: {image.name}");
            Debug.Log($"  Image 路径: {UnityEditor.AssetDatabase.GetAssetPath(image)}");
            withImage++;
        }
        else
        {
            Debug.LogError($"  ❌ Image: NULL");
            Debug.LogError($"  Effect 路径: {UnityEditor.AssetDatabase.GetAssetPath(effect)}");
            withoutImage++;
        }

        Debug.Log($"  Description: {(string.IsNullOrEmpty(effect.Description) ? "(无描述)" : effect.Description)}");
    }

    /// <summary>
    /// 扫描项目中所有的Effect资源
    /// </summary>
    private void ScanAllEffectsInProject()
    {
#if UNITY_EDITOR
        // 查找所有Effect类型的资源
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Effect");

        if (guids.Length == 0)
        {
            Debug.LogWarning("项目中没有找到任何Effect资源");
            return;
        }

        Debug.Log($"找到 {guids.Length} 个Effect资源:\n");

        int withImage = 0;
        int withoutImage = 0;

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Effect effect = UnityEditor.AssetDatabase.LoadAssetAtPath<Effect>(path);

            if (effect != null)
            {
                CheckEffect(effect, ref withImage, ref withoutImage);
            }
        }

        // 总结
        Debug.Log("\n========================================");
        Debug.Log("===== 项目扫描结果 =====");
        Debug.Log($"总共找到: {guids.Length} 个Effect");
        Debug.Log($"✅ 有图标: {withImage} 个");
        Debug.LogError($"❌ 无图标: {withoutImage} 个");
        Debug.Log("========================================");

        if (withoutImage > 0)
        {
            Debug.LogWarning($"\n⚠️ 需要为 {withoutImage} 个Effect配置图标！");
        }
#endif
    }

    /// <summary>
    /// 查找特定名称的Effect并检查
    /// </summary>
    public void CheckEffectByName(string effectName)
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{effectName} t:Effect");

        if (guids.Length == 0)
        {
            Debug.LogWarning($"没有找到名为 '{effectName}' 的Effect");
            return;
        }

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Effect effect = UnityEditor.AssetDatabase.LoadAssetAtPath<Effect>(path);

            if (effect != null && effect.name.Contains(effectName))
            {
                int dummy1 = 0, dummy2 = 0;
                CheckEffect(effect, ref dummy1, ref dummy2);
            }
        }
#endif
    }
}
