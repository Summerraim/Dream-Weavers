using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SkillEffect))]
public class SkillEffectDrawer : PropertyDrawer
{
    const float Line = 18f; // fixed line height
    const float VSpace = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative("Type");
        int lines = 1; // Type
        if (typeProp != null)
        {
            var type = (SkillEffectType)typeProp.enumValueIndex;
            switch (type)
            {
                case SkillEffectType.Damage:
                    lines += 1; // Power
                    break;
                case SkillEffectType.ApplyWeaken:
                    lines += 2; // WeakenRatio + WeakenTurns
                    break;
                case SkillEffectType.ApplyEnhance:
                    lines += 2; // EnhanceRatio + EnhanceTurns
                    break;
            }
        }
        // Add small space between lines
        return lines * Line + (lines - 1) * VSpace;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var r = new Rect(position.x, position.y, position.width, Line);

        // Type
        var typeProp = property.FindPropertyRelative("Type");
        EditorGUI.PropertyField(r, typeProp);
        r.y += Line + VSpace;

        var type = (SkillEffectType)typeProp.enumValueIndex;
        switch (type)
        {
            case SkillEffectType.Damage:
            {
                var powerProp = property.FindPropertyRelative("Power");
                EditorGUI.PropertyField(r, powerProp);
                r.y += Line + VSpace;
                break;
            }
            case SkillEffectType.ApplyWeaken:
            {
                var weakenRatio = property.FindPropertyRelative("WeakenRatio");
                var weakenTurns = property.FindPropertyRelative("WeakenTurns");
                EditorGUI.PropertyField(r, weakenRatio);
                r.y += Line + VSpace;
                EditorGUI.PropertyField(r, weakenTurns);
                r.y += Line + VSpace;
                break;
            }
            case SkillEffectType.ApplyEnhance:
            {
                var enhanceRatio = property.FindPropertyRelative("EnhanceRatio");
                var enhanceTurns = property.FindPropertyRelative("EnhanceTurns");
                EditorGUI.PropertyField(r, enhanceRatio);
                r.y += Line + VSpace;
                EditorGUI.PropertyField(r, enhanceTurns);
                r.y += Line + VSpace;
                break;
            }
        }

        EditorGUI.EndProperty();
    }
}
