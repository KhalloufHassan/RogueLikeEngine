using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Runs a child for at most Duration seconds, stopping it when the timer completes.</summary>
    [Serializable]
    public sealed class TimedAction : BrainNode
    {
        public BrainValue<float> Duration;
        [SerializeReference] public BrainNode Child;
        [NonSerialized] private float m_remaining;

        protected override void OnStart(BrainContext context) => m_remaining = Mathf.Max(0, Duration.Get(context));

        protected override BrainStatus OnTick(BrainContext context)
        {
            if (Child == null) return BrainStatus.Failure;
            if (m_remaining <= 0) return BrainStatus.Success;
            BrainStatus status = Child.Tick(context);
            m_remaining -= context.DeltaTime;
            return status == BrainStatus.Running && m_remaining <= 0 ? BrainStatus.Success : status;
        }

        protected override void OnStop(BrainContext context) => Child?.Reset(context);
    }
}
