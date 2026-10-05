using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public interface IWeaponsSystem : IEntitySystem
    {
        WeaponInstance CurrentWeapon { get; }
        bool CanFire { get; }
        Vector2 AimDirection { get; }
        bool IsAiming { get; }

        void Fire();
        void BeginFire();
        void EndFire();
        void CancelInput();
        void Aim(Vector2 direction);
        void StopAim();

        int CalculatedProjectileDamage(WeaponData weaponData);
        float CalculatedFireRate(WeaponData weaponData);
        float CalculatedProjectileRange(WeaponData weaponData);
    }
}
