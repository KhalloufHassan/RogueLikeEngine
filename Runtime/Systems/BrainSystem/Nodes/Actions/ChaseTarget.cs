using System;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Continuously passes the target's world position to the entity's movement.</summary>
    [Serializable]
    public sealed class ChaseTarget : BrainNode
    {
        public BrainValue<Entity> Target;
        public BrainValue<float> StopDistance;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity target = Target.Get(context);
            IMovement movement = context.Entity.Movement;
            if (!target || target.Health is { IsDead: true } || movement == null) return BrainStatus.Failure;
            Vector3 position = context.Entity.transform.position;
            Vector3 offset = target.transform.position - position;
            offset = Vector3.Project(offset, movement.ToWorld(Vector2.right)) + Vector3.Project(offset, movement.ToWorld(Vector2.up));
            float distance = Mathf.Max(0, StopDistance.Get(context));
            if (offset.sqrMagnitude <= distance * distance)
                StopMovement(context);
            else
                movement.Move(target.transform.position);
            return BrainStatus.Running;
        }

        protected override void OnStop(BrainContext context) => StopMovement(context);

        private void StopMovement(BrainContext context) => context.Entity.Movement?.Move(context.Entity.transform.position);
    }
}
