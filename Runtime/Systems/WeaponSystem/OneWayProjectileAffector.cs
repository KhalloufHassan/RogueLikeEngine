using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>Passes projectiles along a local direction and blocks those travelling against it. Independent of cover or characters.</summary>
    public class OneWayProjectileAffector : MonoBehaviour, IProjectileAffector
    {
        [Tooltip("Local direction bullets may travel through. Use Z for a 3D gate or an XY direction in 2D. Perpendicular shots are blocked.")]
        [SerializeField] private Vector3 m_passDirection = Vector3.forward;

        /// <summary>Allowed world-space travel direction. The Inspector stores it relative to this transform.</summary>
        public Vector3 PassDirection
        {
            get => transform.TransformDirection(m_passDirection).normalized;
            set => m_passDirection = transform.InverseTransformDirection(value).normalized;
        }

        public AffectorResult OnProjectileHit(HitInfo hit, Projectile projectile)
        {
            if (Vector3.Dot(projectile.Direction, PassDirection) > .0001f)
            {
                if (!hit.IsTrigger) projectile.IgnoreCollision(hit.Collider);
                return new AffectorResult { IgnoreCollision = true };
            }
            return default;
        }
    }
}
