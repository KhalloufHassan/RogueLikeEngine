using UnityEngine;

namespace RogueLikeEngine.Systems.Entities
{
    /// <summary>
    /// What a hit touched, independent of 2D/3D physics and of collisions/triggers.
    /// Triggers carry no contact data, so their point is the closest point on the other collider's bounds.
    /// </summary>
    public readonly struct HitInfo
    {
        public GameObject Other { get; }
        /// <summary>The other Collider or Collider2D.</summary>
        public Component Collider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 RelativeVelocity { get; }
        public bool IsTrigger { get; }

        public HitInfo(GameObject other, Component collider, Vector3 point, Vector3 normal, Vector3 relativeVelocity, bool isTrigger)
        {
            Other = other;
            Collider = collider;
            Point = point;
            Normal = normal;
            RelativeVelocity = relativeVelocity;
            IsTrigger = isTrigger;
        }

        public static HitInfo From(Collision collision)
        {
            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            return new HitInfo(collision.gameObject, collision.collider, contact.point, contact.normal, collision.relativeVelocity, false);
        }

        public static HitInfo From(Collision2D collision)
        {
            ContactPoint2D contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            return new HitInfo(collision.gameObject, collision.collider, contact.point, contact.normal, collision.relativeVelocity, false);
        }

        public static HitInfo From(Collider other, Vector3 position)
        {
            Vector3 point = other.bounds.ClosestPoint(position);
            return new HitInfo(other.gameObject, other, point, (position - point).normalized, Vector3.zero, true);
        }

        public static HitInfo From(Collider2D other, Vector3 position)
        {
            Vector3 point = other.bounds.ClosestPoint(position);
            return new HitInfo(other.gameObject, other, point, (position - point).normalized, Vector3.zero, true);
        }
    }
}
