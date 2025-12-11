#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyPool的自定义编辑器 - 提供测试和验证工具
/// </summary>
[CustomEditor(typeof(EnemyPool))]
public class EnemyPoolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyPool pool = (EnemyPool)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧪 测试工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这些按钮仅在编辑器中有效，用于测试对象池配置", MessageType.Info);

        // 验证按钮
        if (GUILayout.Button("✓ 验证对象池配置"))
        {
            bool valid = pool.ValidatePool();
            if (valid)
            {
                EditorUtility.DisplayDialog("验证成功", "对象池配置有效！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证失败", "对象池配置有问题，请查看Console", "确定");
            }
        }

        // 测试随机获取
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎲 测试随机获取"))
        {
            var enemy = pool.GetRandomEnemy();
            if (enemy != null)
            {
                Debug.Log(
                    $"<color=green>随机获取成功:</color> {enemy.DisplayName} (HP:{enemy.MaxHP}, Damage:{enemy.Damage})"
                );
            }
            else
            {
                Debug.LogWarning("随机获取失败 - 对象池可能为空");
            }
        }

        if (GUILayout.Button("⚖️ 测试权重随机"))
        {
            var enemy = pool.GetWeightedRandomEnemy();
            if (enemy != null)
            {
                Debug.Log($"<color=cyan>权重随机成功:</color> {enemy.DisplayName}");
            }
            else
            {
                Debug.LogWarning("权重随机失败");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 测试批量获取
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📦 测试批量获取(3个不重复)"))
        {
            var enemies = pool.GetRandomEnemies(3, false);
            Debug.Log($"<color=yellow>批量获取成功:</color> {enemies.Count} 个敌人");
            foreach (var enemy in enemies)
            {
                Debug.Log($"  - {enemy.DisplayName}");
            }
        }

        if (GUILayout.Button("📦 测试批量获取(5个可重复)"))
        {
            var enemies = pool.GetRandomEnemies(5, true);
            Debug.Log($"<color=yellow>批量获取成功:</color> {enemies.Count} 个敌人");
            foreach (var enemy in enemies)
            {
                Debug.Log($"  - {enemy.DisplayName}");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 统计信息
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📊 统计信息", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"敌人数量: {pool.Count}");
        EditorGUILayout.LabelField($"使用权重: {(pool.UseWeights ? "是" : "否")}");

        if (pool.UseWeights && pool.Weights != null && pool.Weights.Count > 0)
        {
            int totalWeight = 0;
            foreach (var weight in pool.Weights)
            {
                totalWeight += Mathf.Max(0, weight);
            }
            EditorGUILayout.LabelField($"总权重: {totalWeight}");

            // 显示每个敌人的概率
            EditorGUILayout.LabelField("敌人出现概率:", EditorStyles.boldLabel);
            for (int i = 0; i < Mathf.Min(pool.Count, pool.Weights.Count); i++)
            {
                var enemy = pool.GetEnemyByIndex(i);
                if (enemy != null)
                {
                    float probability = (float)pool.Weights[i] / totalWeight * 100f;
                    EditorGUILayout.LabelField($"  {enemy.DisplayName}: {probability:F1}%");
                }
            }
        }
    }
}
#endif
