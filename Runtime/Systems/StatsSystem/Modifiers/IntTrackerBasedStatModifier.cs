using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    /// <summary>Contributes tracker value * increase rate, and recalculates the stat whenever the tracker changes.</summary>
    [Serializable]
    public class IntTrackerBasedStatModifier : AbstractStatModifier
    {
        [SerializeField] private IntTracker m_tracker;
        [SerializeField] private float m_increaseRate;

        public override float Value => m_tracker ? m_tracker.Value * m_increaseRate : 0;
        public override float ValuePreview => Value;

        public override void Configure(StatsStore statsStore, Stat ownerStat)
        {
            UnsubscribeFromTracker();
            base.Configure(statsStore, ownerStat);
            if (m_tracker) m_tracker.OnValueChanged += HandleTrackerChanged;
        }

        public override void Unconfigure()
        {
            UnsubscribeFromTracker();
            base.Unconfigure();
        }

        private void UnsubscribeFromTracker()
        {
            if (m_tracker) m_tracker.OnValueChanged -= HandleTrackerChanged;
        }

        private void HandleTrackerChanged(int _) => OwnerStat?.RecalculateValue();
    }
}
