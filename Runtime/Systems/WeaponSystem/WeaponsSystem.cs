using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class WeaponsSystem : EntitySystem, IWeaponsSystem
    {
        [SerializeField] private StatDefinition m_fireRateStatDefinition;
        [SerializeField] private StatDefinition m_rangeStatDefinition;
        [SerializeField] private StatDefinition m_projectileSpeedStatDefinition;
        [SerializeField] private GunPoint m_gunPoint;

        public virtual WeaponInstance CurrentWeapon => m_gunPoint.CurrentWeapon;
        public virtual bool CanFire => IsSystemActive && m_gunPoint && m_gunPoint.CanFire;
        public Vector3 AimDirection { get; protected set; }
        public bool IsAiming => AimDirection.magnitude > 1E-10;

        public virtual void Fire()
        {
            if(CanFire) m_gunPoint.Fire();
        }

        /// <summary>Called once when the fire action begins, before repeated Fire calls.</summary>
        public virtual void BeginFire() { }

        /// <summary>Called when the player releases the fire action.</summary>
        public virtual void EndFire() { }

        /// <summary>Aborts held input without treating interruption as a release.</summary>
        public virtual void CancelInput() { }
        
        public virtual void Aim(Vector3 direction)
        {
            AimDirection = direction.normalized;
        }
        
        public void StopAim()
        {
            AimDirection = Vector3.zero;
        }
        
        public int CalculatedProjectileDamage(WeaponData weaponData) => weaponData.baseDamage + GetStatValue(weaponData.damageStat);
        public float CalculatedFireRate(WeaponData weaponData) => weaponData.baseFireRate + weaponData.baseFireRate * (GetStatValue(m_fireRateStatDefinition) / 100f);
        public float CalculatedProjectileRange(WeaponData weaponData) => weaponData.baseRange + GetStatValue(m_rangeStatDefinition);
        public float CalculatedProjectileSpeed(WeaponData weaponData) => weaponData.baseProjectileSpeed + weaponData.baseProjectileSpeed * (GetStatValue(m_projectileSpeedStatDefinition) / 100f);

        /// <summary>Returns the entity's value for the stat, or 0 when no stat definition is assigned.</summary>
        private int GetStatValue(StatDefinition statDefinition) => statDefinition ? Entity.StatsStore.GetOrCreateStat(statDefinition).FinalValue : 0;
    }
}
