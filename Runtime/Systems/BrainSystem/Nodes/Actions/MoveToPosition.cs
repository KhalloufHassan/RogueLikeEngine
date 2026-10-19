using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Passes a world destination to a movement until arrival or timeout.</summary>
    [Serializable]
    public sealed class MoveToPosition : BrainNode
    {
        public BrainValue<Vector3> Destination;
        public BrainValue<float> StopDistance = new() { Constant = .2f };
        public BrainValue<float> Timeout = new() { Constant = 6 };
        [NonSerialized] private float m_remaining;

        protected override void OnStart(BrainContext context) => m_remaining = Timeout.Get(context);

        protected override BrainStatus OnTick(BrainContext context)
        {
            if (context.Entity.Movement == null) return BrainStatus.Failure;
            Vector3 destination = Destination.Get(context);
            float distance = Mathf.Max(.01f, StopDistance.Get(context));
            if (Vector3.ProjectOnPlane(destination - context.Entity.transform.position, Vector3.up).sqrMagnitude <= distance * distance)
                return BrainStatus.Success;
            m_remaining -= context.DeltaTime;
            if (m_remaining <= 0) return BrainStatus.Failure;
            context.Entity.Movement.Move(destination);
            return BrainStatus.Running;
        }

        protected override void OnStop(BrainContext context) => context.Entity.Movement?.Move(context.Entity.transform.position);
    }
}
