using System;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Finds a reachable destination farther from a nearby threat.</summary>
    [Serializable]
    public sealed class FindRetreatPosition : BrainNode
    {
        public BrainValue<Entity> Target;
        public BrainValue<float> SafeDistance = new() { Constant = 6 };
        public BrainValue<float> TravelDistance = new() { Constant = 5 };
        public string DestinationVariable;
        public int AgentTypeID;
        [NonSerialized] private NavMeshPath m_path;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity target = Target.Get(context);
            if (!target || target.Health is { IsDead: true }) return BrainStatus.Failure;
            Vector3 origin = context.Entity.transform.position;
            Vector3 away = Vector3.ProjectOnPlane(origin - target.transform.position, Vector3.up);
            float distance = away.magnitude;
            if (distance >= SafeDistance.Get(context)) return BrainStatus.Failure;
            if (distance < .01f) away = context.Entity.transform.forward;
            NavMeshQueryFilter filter = new() { agentTypeID = AgentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(origin, out NavMeshHit start, 1, filter)) return BrainStatus.Failure;
            m_path ??= new NavMeshPath();
            float bestDistance = distance + .5f;
            Vector3 best = origin;
            bool found = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 candidate = origin + Quaternion.Euler(0, i * 45, 0) * away.normalized * TravelDistance.Get(context);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, filter)) continue;
                float separation = Vector3.Distance(hit.position, target.transform.position);
                if (separation <= bestDistance || !NavMesh.CalculatePath(start.position, hit.position, filter, m_path) ||
                    m_path.status != NavMeshPathStatus.PathComplete) continue;
                bestDistance = separation;
                best = hit.position;
                found = true;
            }
            if (found) context.Set(DestinationVariable, best);
            return found ? BrainStatus.Success : BrainStatus.Failure;
        }
    }
}
