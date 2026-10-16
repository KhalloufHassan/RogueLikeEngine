using System;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Finds the nearest living entity on the selected layers and stores it in a variable.</summary>
    [Serializable]
    public sealed class FindTarget : BrainNode
    {
        public string TargetVariable;
        public BrainValue<float> Radius;
        public LayerMask Layers;
        public bool Use2D;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity closest = null;
            Vector3 origin = context.Entity.transform.position;
            float radius = Mathf.Max(0, Radius.Get(context));
            float closestDistance = float.PositiveInfinity;
            if (Use2D)
            {
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin, radius, Layers))
                    Consider(collider.GetComponentInParent<Entity>());
            }
            else
            {
                foreach (Collider collider in Physics.OverlapSphere(origin, radius, Layers, QueryTriggerInteraction.Ignore))
                    Consider(collider.GetComponentInParent<Entity>());
            }

            context.Set(TargetVariable, closest);
            return closest ? BrainStatus.Success : BrainStatus.Failure;

            void Consider(Entity candidate)
            {
                if (!candidate || candidate == context.Entity || candidate.Health is { IsDead: true }) return;
                Vector3 offset = candidate.transform.position - origin;
                if (Use2D) offset.z = 0;
                float distance = offset.sqrMagnitude;
                if (distance >= closestDistance) return;
                closestDistance = distance;
                closest = candidate;
            }
        }
    }
}
