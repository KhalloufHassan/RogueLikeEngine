using RogueLikeEngine.Systems.Entities;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>
    /// Optional damage filter and hit response on the hit object or its nearest parent.
    /// Supports 2D/3D collisions and triggers through HitInfo. Disabled behaviours are not called.
    /// </summary>
    public interface IProjectileAffector
    {
        /// <summary>Called before damage to decide whether damage and the normal collision response apply.</summary>
        AffectorResult OnProjectileHit(HitInfo hit, Projectile projectile);
    }
}
