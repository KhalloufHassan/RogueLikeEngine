using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    public class IntTrackerBasedStatModifier : AbstractStatModifier
    {
        [SerializeField] private IntTracker m_tracker;
        [SerializeField] private float m_increaseRate;

        public override float Value => m_tracker.Value * m_increaseRate;
        public override float ValuePreview => m_tracker.Value * m_increaseRate;
    }
}