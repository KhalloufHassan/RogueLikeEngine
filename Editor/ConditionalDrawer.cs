using System.Reflection;
using RogueLikeEngine.Attributes;
using RogueLikeEngine.Extensions;
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
        PropertyAttribute baseAttribute = attribute;
        ShowIfAttribute showIf = baseAttribute as ShowIfAttribute;

        if (showIf == null)
        {
            return true;
        }

        object parentObject = SerializedPropertyExtensions.GetParentObject(property);
        if (parentObject == null)
        {
            return true;
        }

        FieldInfo conditionField = parentObject.GetType().GetField(
            showIf.ConditionFieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (conditionField == null)
        {
            Debug.LogWarning($"ShowIf: Could not find condition field '{showIf.ConditionFieldName}' on {parentObject.GetType()}");
            return true;
        }

        object conditionValue = conditionField.GetValue(parentObject);
        bool shouldShow;

        if (showIf.CompareValue != null)
        {
            shouldShow = showIf.CompareValue.Equals(conditionValue);
        }
        else if (conditionValue is bool boolVal)
        {
            shouldShow = boolVal;
        }
        else
        {
            shouldShow = conditionValue != null;
        }

        return showIf.Inverse ? !shouldShow : shouldShow;
    }
}


