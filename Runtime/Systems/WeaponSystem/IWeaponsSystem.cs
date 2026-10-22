using System;
using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public interface IWeaponsSystem : IEntitySystem
    {
        WeaponInstance CurrentWeapon { get; }
        bool CanFire { get; }
        Vector3 AimDirection { get; }
        bool IsAiming { get; }
        /// <summary>Whether firing is requested, independently of cooldown or animation.</summary>
        bool IsFireHeld { get; }
        /// <summary>Charge of a held chargeable weapon from 0 to 1.</summary>
        float ChargeProgress { get; }
        bool IsReloading { get; }
        /// <summary>Progress of the current reload from 0 to 1.</summary>
        float ReloadProgress { get; }

        /// <summary>Raised after every successful shot.</summary>
        event Action OnFire;
        /// <summary>Raised after <see cref="Equip"/> replaces the current weapon.</summary>
        event Action OnWeaponChanged;
        event Action OnReloadStarted;
        event Action OnReloadFinished;

        /// <summary>Replaces the current weapon with a new instance of <paramref name="weaponData"/>, or removes it when null.</summary>
        void Equip(WeaponData weaponData);
        void Fire();
        void BeginFire();
        void EndFire();
        void CancelInput();
        void Reload();
        void Aim(Vector3 direction);
        void StopAim();

        int CalculatedProjectileDamage(WeaponData weaponData);
        float CalculatedFireRate(WeaponData weaponData);
        float CalculatedProjectileRange(WeaponData weaponData);
        float CalculatedProjectileSpeed(WeaponData weaponData);
        /// <summary>Total spread angle in degrees; shots deviate up to half of it to either side.</summary>
        float CalculatedInaccuracy(WeaponData weaponData);
        int CalculatedProjectileCount(WeaponData weaponData);
        /// <summary>Rounds per magazine, zero for unlimited ammo.</summary>
        int CalculatedMagazineSize(WeaponData weaponData);
        float CalculatedReloadDuration(WeaponData weaponData);
    }
}
