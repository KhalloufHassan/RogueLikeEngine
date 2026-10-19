using System;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Finds reachable cover on the side away from a threat, near an edge for high cover.</summary>
    [Serializable]
    public sealed class FindCoverPosition : BrainNode
    {
        public BrainValue<Entity> Target;
        public BrainValue<float> Radius = new() { Constant = 15 };
        public BrainValue<float> PreferredDistance = new() { Constant = 8 };
        public string DestinationVariable;
        public LayerMask Layers;
        public int AgentTypeID;
        [NonSerialized] private NavMeshPath m_path;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity target = Target.Get(context);
            if (!target || target.Health is { IsDead: true }) return BrainStatus.Failure;
            Vector3 origin = context.Entity.transform.position;
            NavMeshQueryFilter filter = new() { agentTypeID = AgentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(origin, out NavMeshHit start, 1, filter)) return BrainStatus.Failure;
            m_path ??= new NavMeshPath();
            float bestScore = float.PositiveInfinity;
            Vector3 best = origin;
            foreach (Collider collider in PhysicsUtils.OverlapSphere(origin, Radius.Get(context), Layers, QueryTriggerInteraction.Ignore))
            {
                Cover cover = collider.GetComponentInParent<Cover>();
                if (!cover || !cover.isActiveAndEnabled || collider is not BoxCollider box) continue;
                Transform wall = box.transform;
                Vector3 size = Vector3.Scale(box.size, wall.lossyScale);
                bool thinX = Mathf.Abs(size.x) < Mathf.Abs(size.z);
                Vector3 normal = thinX ? wall.right : wall.forward;
                if (Mathf.Abs(normal.y) > .01f) continue;
                Vector3 center = wall.TransformPoint(box.center);
                if (Vector3.Dot(normal, target.transform.position - center) > 0) normal = -normal;
                Vector3 tangent = Vector3.Cross(Vector3.up, normal);
                float halfWidth = Mathf.Abs(thinX ? size.z : size.x) * .5f;
                float halfDepth = Mathf.Abs(thinX ? size.x : size.z) * .5f;
                for (int side = -1; side <= 1; side++)
                {
                    if (side == 0 && cover.Type == CoverType.High) continue;
                    Vector3 point = center + normal * (halfDepth + .65f) + tangent * (side * Mathf.Max(0, halfWidth - .2f));
                    point.y = origin.y;
                    if (!NavMesh.SamplePosition(point, out NavMeshHit hit, .6f, filter)) continue;
                    Vector3 sight = hit.position + Vector3.up * .5f;
                    Vector3 threat = target.transform.position + Vector3.up * .5f;
                    if (!Physics.Linecast(sight, threat, out RaycastHit obstruction, Layers, QueryTriggerInteraction.Ignore) ||
                        obstruction.collider.GetComponentInParent<Cover>() != cover) continue;
                    float score = Vector3.Distance(origin, hit.position) + Mathf.Max(0, PreferredDistance.Get(context) - Vector3.Distance(hit.position, threat)) * 2;
                    if (score >= bestScore || !NavMesh.CalculatePath(start.position, hit.position, filter, m_path) ||
                        m_path.status != NavMeshPathStatus.PathComplete) continue;
                    bestScore = score;
                    best = hit.position;
                }
            }
            if (float.IsPositiveInfinity(bestScore)) return BrainStatus.Failure;
            context.Set(DestinationVariable, best);
            return BrainStatus.Success;
        }
    }
}
