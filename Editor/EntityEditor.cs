using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Entity))]
public class EntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Assign all components"))
        {
            Entity entity = (Entity) target;
            PopulateSystem<Movement>(serializedObject.FindProperty("m_movement"),entity);
            PopulateSystem<WeaponsSystem>(serializedObject.FindProperty("m_weaponsSystem"),entity);
            PopulateSystem<Health>(serializedObject.FindProperty("m_health"),entity);
            serializedObject.ApplyModifiedProperties();
        }
        
        if (Application.isPlaying && GUILayout.Button("Apply effects"))
        {
            Entity entity = (Entity) target;
            entity.SendMessage("ApplyStartingEffect");
        }
    }

    private static void PopulateSystem<T>(SerializedProperty serializedProperty,Entity entity) where T : EntitySystem
    {
        serializedProperty.objectReferenceValue = entity.GetComponentInChildren<T>();
        if (serializedProperty.objectReferenceValue != null)
        {
            var so = new SerializedObject(serializedProperty.objectReferenceValue);
            so.FindProperty("m_entity").objectReferenceValue = entity;
            so.ApplyModifiedProperties();
        }
    }
}