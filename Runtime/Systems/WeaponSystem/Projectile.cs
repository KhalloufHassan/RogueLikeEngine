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
            base.OnCollisionEnter2D(other);
            DestroyEntity();
            Health health = other.gameObject.GetComponent<Health>();
            if (health)
            {
                health.TakeDamage(Damage);
                Weapon.DamageDealt += Damage.Value;
            }
        }

        #region Pool Implementation
        
        public IPool ParentPool { get; set; }
        public bool IsDisposed { get; set; }

        public void OnRequested()
        {
            
        }

        public void OnDisposed()
        {
            transform.position = new Vector2(10000, 10000);
        }
        
        #endregion

    }
}
