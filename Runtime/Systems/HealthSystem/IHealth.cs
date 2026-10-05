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

        event Action OnHealthChanged;
        event Action<Damage> OnDamaged;
        event Action<int> OnHealed;
        event Action OnDeath;

        void TakeDamage(Damage damage);
        void Heal(int amount);
    }
}
