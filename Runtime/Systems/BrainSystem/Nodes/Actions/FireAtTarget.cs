using System;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Aims and fires at a living target until interrupted or the target becomes invalid.</summary>
    [Serializable]
    public sealed class FireAtTarget : BrainNode
    {
        public BrainValue<Entity> Target;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Entity target = Target.Get(context);
            IWeaponsSystem weapons = context.Entity.WeaponsSystem;
            if (!target || target.Health is { IsDead: true } || weapons == null) return BrainStatus.Failure;
            weapons.Aim(target.transform.position - context.Entity.transform.position);
            if (weapons.CanFire) weapons.Fire();
            return BrainStatus.Running;
        }

        protected override void OnStop(BrainContext context)
        {
            context.Entity.WeaponsSystem?.CancelInput();
            context.Entity.WeaponsSystem?.StopAim();
        }
    }
}
