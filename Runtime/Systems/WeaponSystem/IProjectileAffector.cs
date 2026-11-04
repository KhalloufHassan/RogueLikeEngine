using RogueLikeEngine.Systems.Entities;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>
    /// Optional response on the hit object or its nearest parent, called after the projectile applies damage.
    /// Supports 2D/3D collisions and triggers through HitInfo. Disabled behaviours are not called.
    /// </summary>
    public interface IProjectileAffector
    {
        AffectorResult OnProjectileHit(HitInfo hit, Projectile projectile);
    }
}
