using System;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Systems.Healths
{
    public interface IHealth : IEntitySystem
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsDead { get; }
        /// <summary>Absorbs damage before health. Zero max shield disables it.</summary>
        float CurrentShield { get; }
        float MaxShield { get; }

        /// <summary>While true, all damage is ignored.</summary>
        bool Invulnerable { get; set; }

        event Action OnHealthChanged;
        event Action<Damage> OnDamaged;
        event Action<int> OnHealed;
        event Action OnDeath;
        /// <summary>Raised whenever CurrentShield changes.</summary>
        event Action OnShieldChanged;
        /// <summary>Raised when damage takes the last of the shield.</summary>
        event Action OnShieldBroken;

        void TakeDamage(Damage damage);
        void Heal(int amount);
        void RestoreShield(float amount);
    }
}
