namespace RogueLikeEngine.Systems.Weapons
{
    public struct AffectorResult
    {
        /// <summary>
        /// Skips impact effects, OnHit and automatic despawn. Damage has already been applied.
        /// Does not disable Unity physics contacts; the affector owns any custom response.
        /// </summary>
        public bool IgnoreCollision;
    }
}
