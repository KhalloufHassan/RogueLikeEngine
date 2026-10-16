using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    [Serializable]
    public sealed class Wait : BrainNode
    {
        public BrainValue<float> Duration;
        [NonSerialized] private float m_remaining;

        protected override void OnStart(BrainContext context) => m_remaining = Mathf.Max(0, Duration.Get(context));

        protected override BrainStatus OnTick(BrainContext context)
        {
            m_remaining -= context.DeltaTime;
            return m_remaining > 0 ? BrainStatus.Running : BrainStatus.Success;
        }
    }
}
