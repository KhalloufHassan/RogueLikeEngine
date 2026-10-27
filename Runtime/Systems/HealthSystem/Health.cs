using System;
using RogueLikeEngine.Attributes;
using RogueLikeEngine.Audio;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using RogueLikeEngine.Utils.Timers;
using RogueLikeEngine.Vfx;
using UnityEngine;

namespace RogueLikeEngine.Systems.Healths
{
    public class Health : EntitySystem, IHealth
    {
        [SerializeField] private StatDefinition m_maxHealthStatDefinition;
        [SerializeField] private int m_maxHealth;
        [SerializeField] private FloatingDamagePool m_pool;
        [Tooltip("Destroys the entity when it dies, disable it to handle death yourself through OnDeath (e.g. a game over screen)")]
        [SerializeField] private bool m_destroyEntityOnDeath = true;

        [Header("Shield"), Tooltip("Absorbs damage before health, damage beyond it reaches health. Zero disables shield")]
        [SerializeField, Min(0)] private float m_maxShield;
        [Tooltip("Game seconds without taking damage before shield starts recharging")]
        [SerializeField, Min(0)] private float m_shieldRechargeDelay = 3;
        [Tooltip("Shield regained per game second while recharging")]
        [SerializeField, Min(0)] private float m_shieldRechargeRate = 20;
        [Tooltip("Plays the shield break sound, optional")]
        [SerializeField] private SoundManager m_soundManager;
        [SerializeReference, ReferencePicker] private ISound m_shieldBreakSound;
        [Tooltip("Plays the shield break effect following the entity, optional")]
        [SerializeField] private VfxManager m_vfxManager;
        [SerializeField] private PooledVfx m_shieldBreakVfx;

        public int CurrentHealth { get; private set; }

        /// <summary>Base max health plus the max health stat, follows the stat as it changes.</summary>
        public int MaxHealth => m_maxHealth + (m_maxHealthStat?.FinalValue ?? 0);

        public bool IsDead { get; private set; }
        public float CurrentShield { get; private set; }
        public float MaxShield => m_maxShield;
        public bool Invulnerable { get; set; }

        /// <summary>Raised whenever CurrentHealth or MaxHealth changes.</summary>
        public event Action OnHealthChanged;
        public event Action<Damage> OnDamaged;
        public event Action<int> OnHealed;
        public event Action OnDeath;
        public event Action OnShieldChanged;
        public event Action OnShieldBroken;

        private Stat m_maxHealthStat;
        private int m_lastMaxHealth;
        private FloatTimer m_shieldRechargeTimer;

        private void Awake()
        {
            if (m_maxHealthStatDefinition)
            {
                m_maxHealthStat = Entity.StatsStore.GetOrCreateStat(m_maxHealthStatDefinition);
                m_maxHealthStat.OnValueChanged += HandleMaxHealthChanged;
            }

            m_lastMaxHealth = MaxHealth;
            CurrentHealth = MaxHealth;
            CurrentShield = m_maxShield;
        }

        private void Update()
        {
            if (IsDead || !IsSystemActive || CurrentShield >= m_maxShield) return;
            if (!m_shieldRechargeTimer.IsFinished)
            {
                m_shieldRechargeTimer.Decrement(Time.deltaTime);
                return;
            }
            SetShield(CurrentShield + m_shieldRechargeRate * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (m_maxHealthStat != null)
                m_maxHealthStat.OnValueChanged -= HandleMaxHealthChanged;
        }

        public void TakeDamage(Damage damage)
        {
            if (IsDead || Invulnerable || !IsSystemActive) return;

            int value = Mathf.Max(0, damage.Value);
            m_shieldRechargeTimer = m_shieldRechargeDelay;
            m_shieldRechargeTimer.Reset();
            float absorbed = Mathf.Min(CurrentShield, value);
            if (absorbed > 0)
            {
                SetShield(CurrentShield - absorbed);
                if (CurrentShield <= 0) BreakShield();
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.RoundToInt(value - absorbed));
            if (m_pool) m_pool.Request().Show(damage, transform.position);
            OnDamaged?.Invoke(damage);
            OnHealthChanged?.Invoke();

            if (CurrentHealth == 0) Die();
        }

        public void Heal(int amount)
        {
            if (IsDead || !IsSystemActive || amount <= 0) return;

            int healed = Mathf.Min(amount, MaxHealth - CurrentHealth);
            if (healed <= 0) return;

            CurrentHealth += healed;
            OnHealed?.Invoke(healed);
            OnHealthChanged?.Invoke();
        }

        /// <summary>Adds shield up to its maximum, ignored when dead or the system is locked.</summary>
        public void RestoreShield(float amount)
        {
            if (IsDead || !IsSystemActive || amount <= 0) return;
            SetShield(CurrentShield + amount);
        }

        private void SetShield(float shield)
        {
            shield = Mathf.Clamp(shield, 0, m_maxShield);
            if (Mathf.Approximately(shield, CurrentShield)) return;
            CurrentShield = shield;
            OnShieldChanged?.Invoke();
        }

        private void BreakShield()
        {
            if (m_soundManager && m_shieldBreakSound != null) m_soundManager.Play(m_shieldBreakSound, transform);
            if (m_vfxManager && m_shieldBreakVfx) m_vfxManager.Play(m_shieldBreakVfx, transform);
            OnShieldBroken?.Invoke();
        }

        /// <summary>
        /// Gaining max health also grants the gained amount as current health,
        /// losing it only clamps current health to the new maximum.
        /// </summary>
        private void HandleMaxHealthChanged()
        {
            int maxHealth = MaxHealth;
            int delta = maxHealth - m_lastMaxHealth;
            if (delta == 0 || IsDead) return;

            m_lastMaxHealth = maxHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth + Mathf.Max(0, delta), 0, Mathf.Max(0, maxHealth));
            OnHealthChanged?.Invoke();

            if (CurrentHealth == 0) Die();
        }

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            if (m_destroyEntityOnDeath) Entity.DestroyEntity();
        }
    }
}
