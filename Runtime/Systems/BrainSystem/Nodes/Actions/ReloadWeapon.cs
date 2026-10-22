using System;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Reloads the current weapon, succeeding once its magazine is full. Fails without a weapon or while weapons are locked.</summary>
    [Serializable]
    public sealed class ReloadWeapon : BrainNode
    {
        protected override BrainStatus OnTick(BrainContext context)
        {
            IWeaponsSystem weapons = context.Entity.WeaponsSystem;
            if (weapons?.CurrentWeapon == null) return BrainStatus.Failure;
            weapons.Reload();
            if (weapons.IsReloading) return BrainStatus.Running;
            return weapons.CurrentWeapon.Ammo >= weapons.CalculatedMagazineSize(weapons.CurrentWeapon.WeaponData) ? BrainStatus.Success : BrainStatus.Failure;
        }
    }
}
