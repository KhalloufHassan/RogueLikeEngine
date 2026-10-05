using System;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
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

        public int CurrentHealth { get; private set; }

        /// <summary>Base max health plus the max health stat, follows the stat as it changes.</summary>
        public int MaxHealth => m_maxHealth + (m_maxHealthStat?.FinalValue ?? 0);

        public bool IsDead { get; private set; }

        /// <summary>Raised whenever CurrentHealth or MaxHealth changes.</summary>
        public event Action OnHealthChanged;
        public event Action<Damage> OnDamaged;
        public event Action<int> OnHealed;
        public event Action OnDeath;

        private Stat m_maxHealthStat;
        private int m_lastMaxHealth;

        private void Awake()
        {
            if (m_maxHealthStatDefinition)
            {
                m_maxHealthStat = Entity.StatsStore.GetOrCreateStat(m_maxHealthStatDefinition);
                m_maxHealthStat.OnValueChanged += HandleMaxHealthChanged;
            }

            m_lastMaxHealth = MaxHealth;
            CurrentHealth = MaxHealth;
        }

        private void OnDestroy()
        {
            if (m_maxHealthStat != null)
                m_maxHealthStat.OnValueChanged -= HandleMaxHealthChanged;
        }

        public void TakeDamage(Damage damage)
        {
            if (IsDead || !IsSystemActive) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage.Value));
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
