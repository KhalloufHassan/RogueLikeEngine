using System;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>
    /// Optional custom logic for a weapon, picked on its <see cref="WeaponData"/>. Implementations must be [Serializable].
    /// One instance is shared by every entity using that data, so don't keep per-entity state in it.
    /// </summary>
    [Serializable]
    public abstract class WeaponBehavior
    {
        public virtual void OnEquipped(IWeaponsSystem weapons, WeaponInstance weapon) { }

        /// <summary>Called before any projectile spawns; edit the shot info to change what is launched.</summary>
        public virtual void OnPreFire(IWeaponsSystem weapons, ref ShotInfo shot) { }

        /// <summary>Called for each projectile before it is launched.</summary>
        public virtual void OnProjectileCreated(IWeaponsSystem weapons, in ShotInfo shot, Projectile projectile) { }

        public virtual void OnPostFire(IWeaponsSystem weapons, in ShotInfo shot) { }
        public virtual void OnReloadStarted(IWeaponsSystem weapons, WeaponInstance weapon) { }
        public virtual void OnReloadFinished(IWeaponsSystem weapons, WeaponInstance weapon) { }
    }
}
