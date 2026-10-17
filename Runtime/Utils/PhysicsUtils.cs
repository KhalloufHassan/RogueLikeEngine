using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace RogueLikeEngine.Utils
{
    /// <summary>Pooled overlap queries, executed per enumeration. Foreach releases buffers, including on early exit.</summary>
    public static class PhysicsUtils
    {
        private const int InitialCapacity = 32;

        public static IEnumerable<Collider> OverlapSphere(Vector3 center, float radius, int layerMask = Physics.AllLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            Collider[] colliders = ArrayPool<Collider>.Shared.Rent(InitialCapacity);
            try
            {
                int count;
                while ((count = Physics.OverlapSphereNonAlloc(center, radius, colliders, layerMask, queryTriggerInteraction)) == colliders.Length)
                    GrowBuffer(ref colliders);
                for (int i = 0; i < count; i++) yield return colliders[i];
            }
            finally
            {
                ArrayPool<Collider>.Shared.Return(colliders, true);
            }
        }

        public static IEnumerable<Collider> OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask = Physics.AllLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            Collider[] colliders = ArrayPool<Collider>.Shared.Rent(InitialCapacity);
            try
            {
                int count;
                while ((count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, colliders, layerMask, queryTriggerInteraction)) == colliders.Length)
                    GrowBuffer(ref colliders);
                for (int i = 0; i < count; i++) yield return colliders[i];
            }
            finally
            {
                ArrayPool<Collider>.Shared.Return(colliders, true);
            }
        }

        public static IEnumerable<Collider> OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask = Physics.AllLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            Collider[] colliders = ArrayPool<Collider>.Shared.Rent(InitialCapacity);
            try
            {
                int count;
                while ((count = Physics.OverlapBoxNonAlloc(center, halfExtents, colliders, orientation, layerMask, queryTriggerInteraction)) == colliders.Length)
                    GrowBuffer(ref colliders);
                for (int i = 0; i < count; i++) yield return colliders[i];
            }
            finally
            {
                ArrayPool<Collider>.Shared.Return(colliders, true);
            }
        }

        private static void GrowBuffer(ref Collider[] colliders)
        {
            Collider[] larger = ArrayPool<Collider>.Shared.Rent(checked(colliders.Length * 2));
            ArrayPool<Collider>.Shared.Return(colliders, true);
            colliders = larger;
        }
    }
}
