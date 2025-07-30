using RogueLikeEngine.Attributes;
using UnityEditor;
using UnityEngine;

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
            Debug.Log("Init");
            Store = new();
            foreach (IStatModifier modifier in m_startingModifiers)
            {
                Store.AddModifier(modifier);
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