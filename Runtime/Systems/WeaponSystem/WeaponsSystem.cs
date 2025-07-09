using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class WeaponsSystem : EntitySystem
    {
        [SerializeField] private StatDefinition m_fireRateStatDefinition;
        [SerializeField] private GunPoint m_gunPoint;

        public WeaponInstance CurrentWeapon => m_gunPoint.CurrentWeapon;
        public bool CanFire => m_gunPoint.CanFire;
        public Vector2 AimDirection { get; protected set; }
        public bool IsAiming => AimDirection.magnitude > 1E-10;

        public void Fire()
        {
            if(CanFire) m_gunPoint.Fire();
        }
        
        public virtual void Aim(Vector2 direction)
        {
            AimDirection = direction.normalized;
        }
        
        public void StopAim()
        {
            AimDirection = Vector3.zero;
        }
        
        public int CalculatedProjectileDamage(WeaponData weaponData) => weaponData.baseDamage + Entity.StatsStore.GetOrCreateStat(weaponData.damageStat).FinalValue;
        public float CalculatedFireRate(WeaponData weaponData) => weaponData.baseFireRate + weaponData.baseFireRate * (Entity.StatsStore.GetOrCreateStat(m_fireRateStatDefinition).FinalValue / 100f);
        public int CalculatedProjectileRange(WeaponData weaponData) => weaponData.baseDamage + Entity.StatsStore.GetOrCreateStat(weaponData.damageStat).FinalValue;
    }
}

