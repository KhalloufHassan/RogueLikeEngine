using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class Projectile : Entity,IPoolObject
    {
        public WeaponInstance Weapon { get; set; }
        public Damage Damage { get; set; }
        public float Range { get; set; }
        public void SetDirection(Vector2 direction) => Movement.Move(direction);

        protected override void Update()
        {
            base.Update();
            if (Movement.TraveledDistance >= Range) DestroyEntity();
        }

        protected override void OnCollisionEnter2D(Collision2D other)
        {
            if (IsDisposed) return;
            base.OnCollisionEnter2D(other);
            Health health = other.gameObject.GetComponent<Health>();
            if (health)
            {
                health.TakeDamage(Damage);
                if (Weapon != null) Weapon.DamageDealt += Damage.Value;
            }
            DestroyEntity();
        }

        /// <summary>Pooled projectiles go back to their pool, others are destroyed.</summary>
        public override void DestroyEntity()
        {
            if (ParentPool != null)
                ParentPool.ReturnToPool(this);
            else
                base.DestroyEntity();
        }

        #region Pool Implementation

        public IPool ParentPool { get; set; }
        public bool IsDisposed { get; set; }

        public void OnRequested()
        {
            // First use gets its starting effects from Start, reuses need them re-applied
            if (StartingEffectsApplied) ApplyStartingEffect();
        }

        public void OnDisposed()
        {
            ClearEffects(true);
            if (Movement) Movement.ResetTraveledDistance();
            Weapon = null;
            transform.position = new Vector2(10000, 10000);
        }
        
        #endregion

    }
}
