using System.Reflection;
using RogueLikeEngine.Attributes;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
[CustomPropertyDrawer(typeof(HideIfAttribute))]
public class ConditionalDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShouldDisplay(property))
            return 0f;

        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldDisplay(property))
            EditorGUI.PropertyField(position, property, label, true);
    }

    private bool ShouldDisplay(SerializedProperty property)
    {
        object target = property.serializedObject.targetObject;
        PropertyAttribute baseAttribute = attribute;

        string conditionName;
        object compareValue;
        bool isHide = false;

        if (baseAttribute is ShowIfAttribute show)
        {
            conditionName = show.ConditionFieldName;
            compareValue = show.CompareValue;
        }
        else if (baseAttribute is HideIfAttribute hide)
        {
            conditionName = hide.ConditionFieldName;
            compareValue = hide.CompareValue;
            isHide = true;
        }
        else
        {
            return true;
        }

        FieldInfo field = target.GetType().GetField(conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo prop = target.GetType().GetProperty(conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object conditionValue = null;

        if (field != null)
            conditionValue = field.GetValue(target);
        else if (prop != null)
            conditionValue = prop.GetValue(target, null);
        else
        {
            Debug.LogWarning($"[ShowIf/HideIf] Could not find member '{conditionName}' on {target}");
            return true;
        }

        bool result;

        if (compareValue != null)
        {
            result = compareValue.Equals(conditionValue);
        }
        else if (conditionValue is bool boolVal)
        {
            result = boolVal;
        }
        else
        {
            result = conditionValue != null;
        }

        return isHide ? !result : result;
    }
}
