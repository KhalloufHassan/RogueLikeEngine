using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using RogueLikeEngine.Attributes;

[CustomPropertyDrawer(typeof(ReferencePickerAttribute))]
public class ReferencePickerDrawer : PropertyDrawer
{
    private List<Type> _types;
    private string[] _typeNames;
    private int _selectedIndex;
    private bool _initialized;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!_initialized)
        {
            Initialize(property);
            _initialized = true;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect popupRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        int newIndex = EditorGUI.Popup(popupRect, label.text, _selectedIndex, _typeNames);

        if (newIndex != _selectedIndex)
        {
            _selectedIndex = newIndex;
            Type selectedType = _types[_selectedIndex];

            property.managedReferenceValue = selectedType == null ? null : Activator.CreateInstance(selectedType);
            property.serializedObject.ApplyModifiedProperties();
        }

        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            float y = popupRect.yMax + 2f;
            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect fieldRect = new(position.x, y, position.width, height);
                EditorGUI.PropertyField(fieldRect, iterator, true);
                y += height + 2f;

                if (!iterator.NextVisible(false))
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!_initialized)
            Initialize(property);

        if (property.managedReferenceValue != null)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + 2f;
                if (!iterator.NextVisible(false))
                    break;
            }
        }

        return height;
    }

    private void Initialize(SerializedProperty property)
    {
        Type baseType = GetBaseType(property);

        if (baseType == null)
        {
            _types = new List<Type> { null };
            _typeNames = new[] { "[None]" };
            Debug.LogWarning($"ReferencePicker: Could not resolve base type from {property.managedReferenceFieldTypename}");
            return;
        }

        _types = new List<Type> { null };
        _types.AddRange(GetSubtypes(baseType));

        _typeNames = _types.Select(t => t == null ? "[None]" : t.Name).ToArray();

        if (property.managedReferenceValue != null)
        {
            Type currentType = property.managedReferenceValue.GetType();
            _selectedIndex = _types.FindIndex(t => t == currentType);
            if (_selectedIndex == -1) _selectedIndex = 0;
        }
        else
        {
            _selectedIndex = 0;
        }
    }

    private static Type GetBaseType(SerializedProperty property)
    {
        string typeName = property.managedReferenceFieldTypename;

        if (string.IsNullOrEmpty(typeName))
            return null;

        string[] parts = typeName.Split(' ');
        if (parts.Length != 2)
            return null;

        string assemblyName = parts[0];
        string className = parts[1];

        return Type.GetType($"{className}, {assemblyName}");
    }

    private static IEnumerable<Type> GetSubtypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            })
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);
    }
}
