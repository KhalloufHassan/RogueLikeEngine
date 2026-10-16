using System;
using RogueLikeEngine.Systems.Brains;
using UnityEditor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [CustomEditor(typeof(Brain), true)]
    public sealed class BrainEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_variableOverrides");
            BrainGraph graph = serializedObject.FindProperty("m_graph").objectReferenceValue as BrainGraph;
            if (graph)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Enable an override to set a starting value for this brain. Unchecked variables use the graph's defaults.", MessageType.Info);
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                    DrawVariables(graph);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVariables(BrainGraph graph)
        {
            SerializedProperty overrides = serializedObject.FindProperty("m_variableOverrides");
            using SerializedObject graphObject = new(graph);
            SerializedProperty defaults = graphObject.FindProperty("m_variables");
            for (int i = 0; i < graph.Variables.Length; i++)
            {
                BrainVariable variable = graph.Variables[i];
                SerializedProperty defaultValue = defaults.GetArrayElementAtIndex(i).FindPropertyRelative("Value");
                int index = FindOverride(overrides, variable);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool enabled = EditorGUILayout.Toggle(new GUIContent("", "Override " + variable.Name), index >= 0, GUILayout.Width(18));
                    if (enabled && index < 0)
                    {
                        index = overrides.arraySize++;
                        BrainVariable value = (BrainVariable)Activator.CreateInstance(variable.GetType());
                        value.Name = variable.Name;
                        SerializedProperty item = overrides.GetArrayElementAtIndex(index);
                        item.managedReferenceValue = value;
                        item.FindPropertyRelative("Value").boxedValue = defaultValue.boxedValue;
                    }
                    else if (!enabled && index >= 0)
                    {
                        overrides.DeleteArrayElementAtIndex(index);
                        index = -1;
                    }

                    using (new EditorGUI.DisabledScope(index < 0))
                    {
                        SerializedProperty value = index < 0 ? defaultValue : overrides.GetArrayElementAtIndex(index).FindPropertyRelative("Value");
                        EditorGUILayout.PropertyField(value, new GUIContent(variable.Name), true);
                    }
                }
            }
        }

        private static int FindOverride(SerializedProperty overrides, BrainVariable variable)
        {
            for (int i = 0; i < overrides.arraySize; i++)
            {
                if (overrides.GetArrayElementAtIndex(i).managedReferenceValue is BrainVariable value &&
                    value.Name == variable.Name && value.GetType() == variable.GetType()) return i;
            }
            return -1;
        }
    }
}
