using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Weapons;
using UnityEditor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools
{
    /// <summary>Shows live health and Play Mode testing controls below the health settings.</summary>
    [CustomEditor(typeof(Health), true)]
    public sealed class HealthEditor : Editor
    {
        private int m_healthAmount = 10;
        private float m_shieldAmount = 10;
        private int m_damageAmount = 10;

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
            if (health.MaxShield > 0)
            {
                float shield = playing ? health.CurrentShield / health.MaxShield : 0;
                string shieldLabel = playing ? $"Shield: {health.CurrentShield:0} / {health.MaxShield:0}" : $"Shield: — / {health.MaxShield:0} (not running)";
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), shield, shieldLabel);
            }
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

                if (health.MaxShield > 0)
                {
                    m_shieldAmount = Mathf.Max(1, EditorGUILayout.FloatField("Shield Amount", m_shieldAmount));
                    using (new EditorGUI.DisabledScope(!health.IsSystemActive || health.CurrentShield >= health.MaxShield))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Add Shield")) health.RestoreShield(m_shieldAmount);
                        if (GUILayout.Button(new GUIContent("Max Shield", "Restore the shield to its maximum")))
                            health.RestoreShield(health.MaxShield - health.CurrentShield);
                    }
                }

                m_damageAmount = Mathf.Max(1, EditorGUILayout.IntField("Damage Amount", m_damageAmount));
                using (new EditorGUI.DisabledScope(!health.IsSystemActive || health.Invulnerable))
                    if (GUILayout.Button(new GUIContent("Take Damage", "Shield absorbs it first and restarts its recharge delay")))
                        health.TakeDamage(new Damage(m_damageAmount));
            }
        }
    }
}
