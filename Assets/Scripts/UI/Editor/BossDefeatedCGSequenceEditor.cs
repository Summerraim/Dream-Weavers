#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// BossDefeatedCGSequence的自定义Inspector编辑器
/// 添加测试按钮方便在编辑器中测试动画
/// </summary>
[CustomEditor(typeof(BossDefeatedCGSequence))]
public class BossDefeatedCGSequenceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== 编辑器测试工具 ===", EditorStyles.boldLabel);

        BossDefeatedCGSequence sequence = (BossDefeatedCGSequence)target;

        // 显示配置状态
        EditorGUILayout.LabelField("配置检查:", EditorStyles.boldLabel);

        SerializedProperty cgPanelProp = serializedObject.FindProperty("cgPanel");
        SerializedProperty cgObject1Prop = serializedObject.FindProperty("cgObject1");
        SerializedProperty animator1Prop = serializedObject.FindProperty("animator1");

        bool cgPanelConfigured = cgPanelProp.objectReferenceValue != null;
        bool hasOpeningAnim = cgObject1Prop.objectReferenceValue != null && animator1Prop.objectReferenceValue != null;

        if (cgPanelConfigured)
        {
            EditorGUILayout.HelpBox("✅ CG Panel 已配置", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("❌ CG Panel 未配置！请在上方 'Cg Panel' 字段中拖入 AsmontisCGPanel", MessageType.Error);
        }

        if (!hasOpeningAnim)
        {
            EditorGUILayout.HelpBox("⚠️ 开场动画未完整配置（需要 CgObject1 和 Animator1）", MessageType.Warning);
        }

        EditorGUILayout.Space(5);

        // 测试播放按钮
        GUI.enabled = Application.isPlaying && cgPanelConfigured;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("▶ 测试播放CG序列", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                // 运行时直接播放
                sequence.PlaySequence();
                Debug.Log("[编辑器] 开始播放CG序列测试");
            }
            else
            {
                // 非运行时提示
                EditorUtility.DisplayDialog(
                    "需要运行游戏",
                    "请先点击播放按钮运行游戏，然后再点击此按钮测试CG播放。\n\n或者：\n1. 勾选 Auto Play On Enable\n2. 确保 AsmontisCGPanel 是激活状态\n3. 运行游戏",
                    "知道了"
                );
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space(5);

        // 停止播放按钮
        GUI.enabled = Application.isPlaying;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("⬛ 停止播放", GUILayout.Height(25)))
        {
            if (Application.isPlaying)
            {
                sequence.StopSequence();
                Debug.Log("[编辑器] 停止CG序列播放");
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // 状态显示
        EditorGUILayout.LabelField("当前状态:", EditorStyles.boldLabel);
        string status;
        if (!Application.isPlaying)
        {
            status = "⚠ 未运行（需要先Play游戏）";
        }
        else if (!cgPanelConfigured)
        {
            status = "❌ CG Panel未配置";
        }
        else
        {
            status = sequence.IsPlaying() ? "✅ 正在播放" : "⏸ 待机";
        }
        EditorGUILayout.HelpBox(status, MessageType.Info);

        EditorGUILayout.Space(5);

        // 快速测试说明
        EditorGUILayout.HelpBox(
            "快速测试步骤：\n\n" +
            "【第一次配置】\n" +
            "1. 在上方 'Cg Panel' 字段中拖入 AsmontisCGPanel\n" +
            "2. 配置5个CG物体和对应的Animator\n" +
            "3. 设置动画名称（Animation 1/234/5 Name）\n\n" +
            "【测试动画】\n" +
            "方法1: 确保 AsmontisCGPanel 激活 → 勾选 'Auto Play On Enable' → Play游戏\n" +
            "方法2: Play游戏 → 点击上面的 '▶ 测试播放CG序列' 按钮\n\n" +
            "⚠️ 注意：测试时必须确保 AsmontisCGPanel 是激活状态（SetActive = true）",
            MessageType.None
        );
    }
}
#endif
