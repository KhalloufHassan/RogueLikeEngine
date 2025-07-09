using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

namespace RogueLikeEngine.Systems.Healths
{
    public class Health : EntitySystem
    {
        [SerializeField] private StatDefinition m_maxHealthStatDefinition;
        [SerializeField] private int m_maxHealth;
        [SerializeField] private FloatingDamagePool m_pool;
        
        public int CurrentHealth { get; private set; }
        
        private void Awake()
        {
            CurrentHealth = m_maxHealth + Entity.StatsStore.GetOrCreateStat(m_maxHealthStatDefinition).FinalValue;
        }

        public void TakeDamage(Damage damage)
        {
            CurrentHealth -= damage.Value;
            m_pool.Request().Show(damage,transform.position);
            if(CurrentHealth <= 0) Entity.DestroyEntity();
        }
    }
}