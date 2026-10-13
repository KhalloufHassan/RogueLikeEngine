namespace RogueLikeEngine.Systems.Entities
{
    public interface IEntitySystem
    {
        Entity Entity { get; }

        /// <summary>False when the system was deactivated or while any lock is held.</summary>
        bool IsSystemActive { get; set; }

        /// <summary>
        /// Deactivates the system until every owner that locked it unlocks it,
        /// so overlapping sources (e.g. a stun and an ability) don't re-enable it for each other.
        /// </summary>
        void Lock(object owner);

        void Unlock(object owner);
    }
}
