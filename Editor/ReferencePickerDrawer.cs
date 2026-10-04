using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using RogueLikeEngine.Attributes;

/// <summary>
/// Draws a type picker for [SerializeReference] fields followed by the selected instance's fields.
/// Keeps no per-property state: Unity shares one drawer instance across all elements of an array,
/// so the current selection is always read from the property itself.
/// </summary>
[CustomPropertyDrawer(typeof(ReferencePickerAttribute))]
public class ReferencePickerDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    private class TypeOptions
    {
        public Type[] Types;
        public GUIContent[] Names;
        /// <summary>Type names in the "Assembly Namespace.Class" format of managedReferenceFullTypename</summary>
        public string[] ManagedNames;
    }

    private static readonly Dictionary<string, TypeOptions> s_optionsByFieldType = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, $"[{nameof(ReferencePickerAttribute)}] requires [SerializeReference]");
            EditorGUI.EndProperty();
            return;
        }

        TypeOptions options = GetOptions(property);
        Rect popupRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int currentIndex = Array.IndexOf(options.ManagedNames, property.managedReferenceFullTypename);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUI.Popup(popupRect, label, currentIndex, options.Names);

        if (newIndex != currentIndex)
        {
            Type selectedType = options.Types[newIndex];
            property.managedReferenceValue = selectedType == null ? null : Activator.CreateInstance(selectedType);
            property.serializedObject.ApplyModifiedProperties();
        }
        else if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            float y = popupRect.yMax + Spacing;
            foreach (SerializedProperty child in GetChildren(property))
            {
                float height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                y += height + Spacing;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.propertyType != SerializedPropertyType.ManagedReference || property.managedReferenceValue == null)
            return height;

        foreach (SerializedProperty child in GetChildren(property))
            height += EditorGUI.GetPropertyHeight(child, true) + Spacing;

        return height;
    }

    private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
    {
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        if (!iterator.NextVisible(true)) yield break;

        while (!SerializedProperty.EqualContents(iterator, end))
        {
            yield return iterator;
            if (!iterator.NextVisible(false)) yield break;
        }
    }

    private static TypeOptions GetOptions(SerializedProperty property)
    {
        string fieldTypeName = property.managedReferenceFieldTypename;
        if (s_optionsByFieldType.TryGetValue(fieldTypeName, out TypeOptions options))
            return options;

        Type baseType = ResolveType(fieldTypeName);
        if (baseType == null)
            Debug.LogWarning($"ReferencePicker: Could not resolve base type from '{fieldTypeName}'");

        List<Type> types = new() { null };
        if (baseType != null)
        {
            types.AddRange(TypeCache.GetTypesDerivedFrom(baseType)
                .Where(IsAssignableToReference)
                .OrderBy(t => t.Name));
        }

        options = new TypeOptions
        {
            Types = types.ToArray(),
            Names = types.Select(t => new GUIContent(t == null ? "[None]" : ObjectNames.NicifyVariableName(t.Name), t?.FullName)).ToArray(),
            ManagedNames = types.Select(t => t == null ? string.Empty : $"{t.Assembly.GetName().Name} {t.FullName?.Replace('+', '/')}").ToArray()
        };
        s_optionsByFieldType[fieldTypeName] = options;
        return options;
    }

    /// <summary>Same rules [SerializeReference] has: concrete, non generic, [Serializable], not a Unity object, default constructible.</summary>
    private static bool IsAssignableToReference(Type type) =>
        !type.IsAbstract &&
        !type.IsInterface &&
        !type.IsGenericTypeDefinition &&
        type.IsSerializable &&
        !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
        type.GetConstructor(Type.EmptyTypes) != null;

    /// <summary>Resolves Unity's "Assembly Namespace.Class" format, where nested types use '/' instead of '+'.</summary>
    private static Type ResolveType(string managedTypeName)
    {
        if (string.IsNullOrEmpty(managedTypeName))
            return null;

        int separator = managedTypeName.IndexOf(' ');
        if (separator < 0)
            return null;

        string assemblyName = managedTypeName[..separator];
        string className = managedTypeName[(separator + 1)..].Replace('/', '+');
        return Type.GetType($"{className}, {assemblyName}");
    }
}
