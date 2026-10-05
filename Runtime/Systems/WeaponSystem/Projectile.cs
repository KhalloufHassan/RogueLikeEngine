using System.Collections;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour,IPoolObject
    {
        public WeaponInstance Weapon { get; set; }
        public Damage Damage { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public Vector2 Direction { get; private set; }
        public float TraveledDistance { get; private set; }

        public Entity Owner { get; set; }

        /// <summary>Optional Entity on the same object, hosting the projectile's effects.</summary>
        public Entity Entity { get; private set; }

        private Rigidbody2D m_rigidbody;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            Entity = GetComponent<Entity>();
        }

        public void SetDirection(Vector2 direction) => Direction = direction.normalized;

        private void FixedUpdate()
        {
            m_rigidbody.linearVelocity = Direction * Speed;
            TraveledDistance += Speed * Time.fixedDeltaTime;
            if (TraveledDistance >= Range) Finish();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (IsDisposed || !enabled) return;
            Entity target = other.gameObject.GetComponentInParent<Entity>();
            IHealth health = target && target != Owner ? target.Health : null;
            if (health != null)
            {
                health.TakeDamage(Damage);
                if (Weapon != null) Weapon.DamageDealt += Damage.Value;
            }
            Finish();
        }

        /// <summary>
        /// Stops the projectile and despawns it next frame, so every component on it (e.g. its Entity's effects)
        /// still receives the current collision.
        /// </summary>
        private void Finish()
        {
            StartCoroutine(DespawnNextFrame());
            enabled = false;
            m_rigidbody.linearVelocity = Vector2.zero;
        }

        private IEnumerator DespawnNextFrame()
        {
            yield return null;
            Despawn();
        }

        /// <summary>Pooled projectiles go back to their pool, others are destroyed.</summary>
        public void Despawn()
        {
            if (ParentPool != null)
                ParentPool.ReturnToPool(this);
            else
                Destroy(gameObject);
        }

        #region Pool Implementation

        public IPool ParentPool { get; set; }
        public bool IsDisposed { get; set; }

        public void OnRequested()
        {
            enabled = true;
            if (Entity) Entity.ApplyStartingEffect();
        }

        public void OnDisposed()
        {
            if (Entity) Entity.ClearEffects(true);
            TraveledDistance = 0;
            Direction = Vector2.zero;
            Weapon = null;
            Owner = null;
            transform.position = new Vector2(10000, 10000);
        }

        #endregion

    }
}
