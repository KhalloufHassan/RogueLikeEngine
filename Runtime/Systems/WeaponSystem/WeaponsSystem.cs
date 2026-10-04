using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class WeaponsSystem : EntitySystem
    {
        [SerializeField] private StatDefinition m_fireRateStatDefinition;
        [SerializeField] private GunPoint m_gunPoint;

        public virtual WeaponInstance CurrentWeapon => m_gunPoint.CurrentWeapon;
        public virtual bool CanFire => IsSystemActive && m_gunPoint && m_gunPoint.CanFire;
        public Vector2 AimDirection { get; protected set; }
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
        public float CalculatedProjectileRange(WeaponData weaponData) => weaponData.baseRange + Entity.StatsStore.GetOrCreateStat(weaponData.damageStat).FinalValue;
    }
}
