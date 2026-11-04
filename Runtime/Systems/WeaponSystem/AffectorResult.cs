namespace RogueLikeEngine.Systems.Weapons
{
    public struct AffectorResult
    {
        /// <summary>Skips damage to the hit object without changing the collision response.</summary>
        public bool IgnoreDamage;

        /// <summary>
        /// Skips impact effects, OnHit and automatic despawn.
        /// Does not disable Unity physics contacts; the affector owns any custom response.
        /// </summary>
        public bool IgnoreCollision;
    }
}
