using System;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>
    /// Aims and fires at a living target until interrupted, failing when the target becomes invalid.
    /// Succeeds once the weapon starts reloading, so an empty magazine ends the burst.
    /// </summary>
    [Serializable]
    public sealed class FireAtTarget : BrainNode
    {
        public BrainValue<Entity> Target;

        protected override void OnStart(BrainContext context) => context.Entity.WeaponsSystem?.BeginFire();

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity target = Target.Get(context);
            IWeaponsSystem weapons = context.Entity.WeaponsSystem;
            if (!target || target.Health is { IsDead: true } || weapons == null) return BrainStatus.Failure;
            weapons.Aim(target.transform.position - context.Entity.transform.position);
            if (weapons.IsReloading) return BrainStatus.Success;
            if (!weapons.CanFire) return BrainStatus.Running;
            if (weapons.CurrentWeapon is not { WeaponData: { chargeable: true } })
                weapons.Fire();
            else if (weapons.ChargeProgress >= 1)
            {
                weapons.EndFire();
                weapons.BeginFire();
            }
            return BrainStatus.Running;
        }

        protected override void OnStop(BrainContext context)
        {
            context.Entity.WeaponsSystem?.CancelInput();
            context.Entity.WeaponsSystem?.StopAim();
        }
    }
}
