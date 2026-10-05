using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class WeaponInstance
    {
        public WeaponInstance(WeaponData weaponData)
        {
            WeaponData = weaponData;
        }
        
        public WeaponData WeaponData { get; }
        public int ProjectilesFired { get; private set; }
        public float DamageDealt { get; set; }
        
        
        public Projectile CreateProjectile(IWeaponsSystem ownerSystem)
        {
            Projectile projectile = WeaponData.projectilesPool ? WeaponData.projectilesPool.Request() : Object.Instantiate(WeaponData.projectilePrefab);
            projectile.Damage = new Damage(ownerSystem.CalculatedProjectileDamage(WeaponData));
            projectile.Range = ownerSystem.CalculatedProjectileRange(WeaponData);
            projectile.Weapon = this;
            projectile.Owner = ownerSystem.Entity;
            ProjectilesFired++;
            return projectile;
        }
        
    }
}