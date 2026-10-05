using RogueLikeEngine.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueLikeEngine.Systems.Stats
{
    [CreateAssetMenu(fileName = nameof(GlobalStatsStore),menuName = "Stats/" + nameof(GlobalStatsStore))]
    public class GlobalStatsStore : ScriptableObject
    {
        [SerializeReference, ReferencePicker]
        private IStatModifier[] m_startingModifiers;
        public StatsStore Store { get; private set; }

        private void OnEnable()
        {
            InitializeStore();
            
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
            #endif
        }

        private void InitializeStore()
        {
            Store = new();
            if (m_startingModifiers == null) return;

            foreach (IStatModifier modifier in m_startingModifiers)
            {
                if (modifier?.TargetStat) Store.AddModifier(modifier);
            }
        }
        
#if UNITY_EDITOR

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
        }

        private void HandleOnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode  && EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
            {
                InitializeStore();
            }
        }
#endif
    }
}