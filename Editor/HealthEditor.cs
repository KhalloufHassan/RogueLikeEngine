using RogueLikeEngine.Systems.Healths;
using UnityEditor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools
{
    /// <summary>Shows live health and Play Mode testing controls below the health settings.</summary>
    [CustomEditor(typeof(Health), true)]
    public sealed class HealthEditor : Editor
    {
        private int m_healthAmount = 10;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Health health = (Health)target;
            bool playing = Application.IsPlaying(health.gameObject);
            int maximum = health.MaxHealth;
            float fraction = playing && maximum > 0 ? Mathf.Clamp01((float)health.CurrentHealth / maximum) : 0;
            string label = playing ? $"Health: {health.CurrentHealth} / {maximum}" : $"Health: — / {maximum} (not running)";
            if (playing && health.IsDead) label += " (Dead)";

            EditorGUILayout.Space();
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 22), fraction, label);
            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(.5f, .5f, .5f, .5f));
            EditorGUILayout.LabelField("Testing Section", EditorStyles.boldLabel);
            if (!playing) EditorGUILayout.HelpBox("Enter Play Mode to use the testing controls.", MessageType.Info);

            using (new EditorGUI.DisabledScope(!playing || health.IsDead))
            {
                if (GUILayout.Button(health.Invulnerable ? "Disable Invulnerability" : "Set Invulnerable"))
                    health.Invulnerable = !health.Invulnerable;

                m_healthAmount = Mathf.Max(1, EditorGUILayout.IntField("Health Amount", m_healthAmount));
                using (new EditorGUI.DisabledScope(!health.IsSystemActive || health.CurrentHealth >= maximum))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Health")) health.Heal(m_healthAmount);
                    if (GUILayout.Button(new GUIContent("Max Health", "Restore current health to its maximum")))
                        health.Heal(maximum - health.CurrentHealth);
                }
            }
        }
    }
}
