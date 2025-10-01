#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FloatMinMaxDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var minProp = property.FindPropertyRelative("min");
        var maxProp = property.FindPropertyRelative("max");

        EditorGUI.BeginProperty(position, label, property);

        // Label
        position = EditorGUI.PrefixLabel(position, label);

        float fieldWidth = (position.width - 6f) / 2f;
        var minRect = new Rect(position.x, position.y, fieldWidth, position.height);
        var maxRect = new Rect(position.x + fieldWidth + 6f, position.y, fieldWidth, position.height);

        // Min / Max fields
        minProp.floatValue = EditorGUI.FloatField(minRect, minProp.floatValue);
        maxProp.floatValue = EditorGUI.FloatField(maxRect, maxProp.floatValue);

        EditorGUI.EndProperty();
    }
}
#endif
