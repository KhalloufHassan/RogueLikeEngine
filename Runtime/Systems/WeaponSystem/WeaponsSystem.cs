using System;
using RogueLikeEngine.Audio;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Utils.Timers;
using RogueLikeEngine.Vfx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RogueLikeEngine.Systems.Weapons
{
    public class WeaponsSystem : EntitySystem, IWeaponsSystem
    {
        [SerializeField] private WeaponData m_weaponData;
        [SerializeField] private GunPoints m_gunPoints;
        [Tooltip("Plays the weapons' fire sounds, optional")]
        [SerializeField] private SoundManager m_soundManager;
        [Tooltip("Plays the weapons' muzzle flashes, optional")]
        [SerializeField] private VfxManager m_vfxManager;
        [SerializeField] private StatDefinition m_fireRateStatDefinition;
        [SerializeField] private StatDefinition m_rangeStatDefinition;
        [SerializeField] private StatDefinition m_projectileSpeedStatDefinition;
        [Tooltip("Reduces the weapon's inaccuracy by its value as a percentage")]
        [SerializeField] private StatDefinition m_accuracyStatDefinition;
        [Tooltip("Adds its value to the projectiles of every shot")]
        [SerializeField] private StatDefinition m_projectileCountStatDefinition;
        [Tooltip("Adds its value to the magazine size of weapons with limited ammo")]
        [SerializeField] private StatDefinition m_magazineSizeStatDefinition;
        [Tooltip("Reduces the reload duration by its value as a percentage")]
        [SerializeField] private StatDefinition m_reloadSpeedStatDefinition;

        public WeaponInstance CurrentWeapon { get; private set; }
        public virtual bool CanFire => IsSystemActive && CurrentWeapon != null && m_gunPoints && m_coolDownTimer.IsFinished && !IsReloading && HasAmmo && !IsHiddenInCover;
        public Vector3 AimDirection { get; protected set; }
        public bool IsAiming => AimDirection.magnitude > 1E-10;
        public bool IsFireHeld { get; private set; }
        public float ChargeProgress { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadProgress => IsReloading && m_reloadDuration > 0 ? 1 - m_reloadRemaining / m_reloadDuration : 0;

        public event Action OnFire;
        public event Action OnWeaponChanged;
        public event Action OnReloadStarted;
        public event Action OnReloadFinished;

        /// <summary>Behind cover without exposure, where shots would only hit the cover.</summary>
        protected bool IsHiddenInCover => Entity && Entity.Movement is { IsInCover: true, IsExposed: false };
        private bool HasAmmo => CalculatedMagazineSize(CurrentWeapon.WeaponData) <= 0 || CurrentWeapon.Ammo > 0;

        private AutoTimer m_coolDownTimer;
        private float m_reloadDuration;
        private float m_reloadRemaining;

        protected virtual void Awake() => Equip(m_weaponData);

        protected virtual void Update()
        {
            if (CurrentWeapon == null) return;
            if (IsReloading && (m_reloadRemaining -= Time.deltaTime) <= 0) FinishReload();
            else if (!HasAmmo) Reload();
            if (!IsFireHeld) return;

            WeaponData weaponData = CurrentWeapon.WeaponData;
            if (!weaponData.chargeable && weaponData.automatic)
                Fire();
            else if (weaponData.chargeable && !IsReloading)
                ChargeProgress = weaponData.chargeDuration > 0 ? Mathf.Min(1, ChargeProgress + Time.deltaTime / weaponData.chargeDuration) : 1;
        }

        public void Equip(WeaponData weaponData)
        {
            IsReloading = false;
            ChargeProgress = 0;
            CurrentWeapon = weaponData ? weaponData.CreateWeaponInstance() : null;
            if (CurrentWeapon != null)
            {
                CurrentWeapon.Ammo = CalculatedMagazineSize(weaponData);
                weaponData.behavior?.OnEquipped(this, CurrentWeapon);
            }
            OnWeaponChanged?.Invoke();
        }

        /// <summary>Fires a single shot when ready. Holding fire repeats it for automatic weapons, releasing it fires chargeable ones.</summary>
        public virtual void Fire()
        {
            if (!CanFire) return;
            WeaponData weaponData = CurrentWeapon.WeaponData;
            float charge = weaponData.chargeable ? ChargeProgress : 0;
            ShotInfo shot = new()
            {
                Weapon = CurrentWeapon,
                Charge = charge,
                ProjectileCount = CalculatedProjectileCount(weaponData),
                SpreadAngle = weaponData.spreadAngle,
                Inaccuracy = CalculatedInaccuracy(weaponData),
                DamageMultiplier = Mathf.Lerp(1, weaponData.fullChargeDamageMultiplier, charge)
            };
            weaponData.behavior?.OnPreFire(this, ref shot);

            Vector3 position = m_gunPoints.Position;
            Vector3 forward = IsAiming ? AimDirection : m_gunPoints.GetDirection(0);
            for (int i = 0; i < shot.ProjectileCount; i++)
            {
                Projectile projectile = CurrentWeapon.CreateProjectile(this);
                projectile.Damage = new Damage(Mathf.RoundToInt(projectile.Damage.Value * shot.DamageMultiplier));
                weaponData.behavior?.OnProjectileCreated(this, shot, projectile);
                float spread = shot.ProjectileCount > 1 ? (i / (shot.ProjectileCount - 1f) - .5f) * shot.SpreadAngle : 0;
                projectile.Launch(position, m_gunPoints.GetDirection(forward, spread + Random.Range(-.5f, .5f) * shot.Inaccuracy));
            }
            PlayFireEffects(weaponData);
            m_gunPoints.Advance();

            ChargeProgress = 0;
            if (CalculatedMagazineSize(weaponData) > 0) CurrentWeapon.Ammo--;
            m_coolDownTimer = 1f / CalculatedFireRate(weaponData);
            m_coolDownTimer.Reset();
            weaponData.behavior?.OnPostFire(this, shot);
            OnFire?.Invoke();
        }

        private void PlayFireEffects(WeaponData weaponData)
        {
            Transform muzzle = m_gunPoints.Current;
            if (m_soundManager && weaponData.fireSound != null) m_soundManager.Play(weaponData.fireSound, muzzle);
            if (m_vfxManager && weaponData.muzzleFlash) m_vfxManager.Play(weaponData.muzzleFlash, muzzle);
        }

        /// <summary>Starts refilling the magazine, unless it is full, already reloading or the ammo is unlimited.</summary>
        public virtual void Reload()
        {
            if (!IsSystemActive || CurrentWeapon == null || IsReloading) return;
            WeaponData weaponData = CurrentWeapon.WeaponData;
            if (CurrentWeapon.Ammo >= CalculatedMagazineSize(weaponData)) return;
            IsReloading = true;
            ChargeProgress = 0;
            m_reloadDuration = m_reloadRemaining = CalculatedReloadDuration(weaponData);
            weaponData.behavior?.OnReloadStarted(this, CurrentWeapon);
            OnReloadStarted?.Invoke();
        }

        private void FinishReload()
        {
            IsReloading = false;
            CurrentWeapon.Ammo = CalculatedMagazineSize(CurrentWeapon.WeaponData);
            CurrentWeapon.WeaponData.behavior?.OnReloadFinished(this, CurrentWeapon);
            OnReloadFinished?.Invoke();
        }

        /// <summary>Called once when the fire action begins. Fires the first shot of weapons that aren't chargeable.</summary>
        public virtual void BeginFire()
        {
            IsFireHeld = IsSystemActive && isActiveAndEnabled;
            ChargeProgress = 0;
            if (IsFireHeld && CurrentWeapon is { WeaponData: { chargeable: false } }) Fire();
        }

        /// <summary>Called when the fire action is released. Fires chargeable weapons with their current charge.</summary>
        public virtual void EndFire()
        {
            if (IsFireHeld && CurrentWeapon is { WeaponData: { chargeable: true } }) Fire();
            CancelInput();
        }

        /// <summary>Aborts held input without treating interruption as a release.</summary>
        public virtual void CancelInput()
        {
            IsFireHeld = false;
            ChargeProgress = 0;
        }

        protected override void OnSystemActiveChanged()
        {
            if (IsSystemActive) return;
            CancelInput();
            IsReloading = false;
        }

        protected virtual void OnDisable() => CancelInput();

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
        public float CalculatedInaccuracy(WeaponData weaponData) => Mathf.Max(0f, weaponData.baseInaccuracy - weaponData.baseInaccuracy * (GetStatValue(m_accuracyStatDefinition) / 100f));
        public int CalculatedProjectileCount(WeaponData weaponData) => Mathf.Max(1, weaponData.baseProjectileCount + GetStatValue(m_projectileCountStatDefinition));
        public int CalculatedMagazineSize(WeaponData weaponData) => weaponData.baseMagazineSize > 0 ? Mathf.Max(1, weaponData.baseMagazineSize + GetStatValue(m_magazineSizeStatDefinition)) : 0;
        public float CalculatedReloadDuration(WeaponData weaponData) => Mathf.Max(0f, weaponData.baseReloadDuration - weaponData.baseReloadDuration * (GetStatValue(m_reloadSpeedStatDefinition) / 100f));

        /// <summary>Returns the entity's value for the stat, or 0 when no stat definition is assigned.</summary>
        private int GetStatValue(StatDefinition statDefinition) => statDefinition ? Entity.StatsStore.GetOrCreateStat(statDefinition).FinalValue : 0;
    }
}
