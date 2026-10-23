using System;
using RogueLikeEngine.Attributes;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    [CreateAssetMenu(fileName = nameof(WeaponData), menuName = "Weapons/" + nameof(WeaponData))]
    public class WeaponData : ScriptableObject
    {
        public StatDefinition damageStat;
        public string weaponName;
        public string description;
        public Sprite icon;
        public int baseDamage;
        public float baseFireRate;
        public float baseRange;
        public float baseProjectileSpeed = 10;
        [Tooltip("Total spread angle in degrees; shots deviate up to half of it to either side")]
        public float baseInaccuracy;
        [Tooltip("Projectiles launched by every shot")]
        public int baseProjectileCount = 1;
        [Tooltip("Angle in degrees the projectiles of one shot are spread evenly across")]
        public float spreadAngle;
        [Tooltip("Keeps firing while fire is held, otherwise fires once per press. Ignored by chargeable weapons")]
        public bool automatic = true;
        [Tooltip("Charges while fire is held and fires on release")]
        public bool chargeable;
        [ShowIf(nameof(chargeable)), Tooltip("Seconds to reach a full charge")]
        public float chargeDuration = 1;
        [ShowIf(nameof(chargeable)), Tooltip("Damage multiplier of a full charge, partial charges scale linearly from 1")]
        public float fullChargeDamageMultiplier = 2;
        [Tooltip("Rounds per magazine, zero for unlimited ammo")]
        public int baseMagazineSize;
        [HideIf(nameof(baseMagazineSize), 0)]
        public float baseReloadDuration = 1;
        [Range(0, 1), Tooltip("Movement speed lost while aiming: 0 doesn't slow, 1 stops movement")]
        public float weight;
        [Tooltip("Pooled automatically, the pool is created on the first shot")]
        public Projectile projectilePrefab;
        [SerializeReference, ReferencePicker, Tooltip("Optional custom logic called around firing and reloading")]
        public WeaponBehavior behavior;

        [NonSerialized] private ProjectilesPool m_projectilesPool;

        public virtual WeaponInstance CreateWeaponInstance() => new(this);

        /// <summary>Takes a projectile from this weapon's pool, created on first use or when the prefab changes.</summary>
        public Projectile RequestProjectile()
        {
            if (!m_projectilesPool || m_projectilesPool.prefab != projectilePrefab)
            {
                m_projectilesPool = CreateInstance<ProjectilesPool>();
                m_projectilesPool.hideFlags = HideFlags.DontUnloadUnusedAsset;
                m_projectilesPool.prefab = projectilePrefab;
            }

            return m_projectilesPool.Request();
        }
    }
}
