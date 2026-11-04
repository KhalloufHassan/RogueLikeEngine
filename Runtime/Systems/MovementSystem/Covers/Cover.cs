using System.Collections.Generic;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public class Cover : MonoBehaviour, IProjectileAffector
    {
        [SerializeField] private CoverType m_type;

        private readonly HashSet<CoverMovement> m_occupants = new();

        public CoverType Type => m_type;
        public bool IsAvailable { get; internal set; }

        internal void Register(CoverMovement movement) => m_occupants.Add(movement);
        internal void Unregister(CoverMovement movement) => m_occupants.Remove(movement);

        public AffectorResult OnProjectileHit(HitInfo hit, Projectile projectile)
        {
            if (m_type != CoverType.Low) return default;
            foreach (CoverMovement movement in m_occupants)
                if (movement && movement.isActiveAndEnabled && movement.IsInCover && movement.ExposureProgress <= 0
                    && movement.Entity && movement.Entity.Health is { IsDead: false }
                    && Vector3.Dot(projectile.Direction, movement.CoverNormal) > .0001f) return default;
            projectile.IgnoreCollision(hit.Collider);
            return new AffectorResult { IgnoreCollision = true };
        }

        private void OnDisable() => IsAvailable = false;
    }
}
