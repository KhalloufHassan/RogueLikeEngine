using System;
using System.Collections.Generic;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Tests.PlayMode
{
    [Serializable]
    public sealed class RecordingWeaponBehavior : WeaponBehavior
    {
        public readonly List<Projectile> Projectiles = new();
        public int Equipped, PreFires, PostFires, ReloadsStarted, ReloadsFinished;

        public override void OnEquipped(IWeaponsSystem weapons, WeaponInstance weapon) => Equipped++;
        public override void OnPreFire(IWeaponsSystem weapons, ref ShotInfo shot) => PreFires++;
        public override void OnProjectileCreated(IWeaponsSystem weapons, in ShotInfo shot, Projectile projectile) => Projectiles.Add(projectile);
        public override void OnPostFire(IWeaponsSystem weapons, in ShotInfo shot) => PostFires++;
        public override void OnReloadStarted(IWeaponsSystem weapons, WeaponInstance weapon) => ReloadsStarted++;
        public override void OnReloadFinished(IWeaponsSystem weapons, WeaponInstance weapon) => ReloadsFinished++;
    }
}
