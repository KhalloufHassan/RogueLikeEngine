using System.Collections;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class Projectile : MonoBehaviour,IPoolObject
    {
        public WeaponInstance Weapon { get; set; }
        public Damage Damage { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public Vector3 Direction { get; private set; }
        public float TraveledDistance { get; private set; }

        public Entity Owner { get; set; }

        /// <summary>Optional Entity on the same object, hosting the projectile's effects.</summary>
        public Entity Entity { get; private set; }

        private Rigidbody2D m_rigidbody2D;
        private Rigidbody m_rigidbody3D;

        private void Awake()
        {
            m_rigidbody2D = GetComponent<Rigidbody2D>();
            m_rigidbody3D = GetComponent<Rigidbody>();
            Entity = GetComponent<Entity>();
        }

        public void SetDirection(Vector3 direction) => Direction = direction.normalized;

        private void FixedUpdate()
        {
            SetVelocity(Direction * Speed);
            TraveledDistance += Speed * Time.fixedDeltaTime;
            if (TraveledDistance >= Range) Finish();
        }

        private void OnCollisionEnter(Collision collision) => HandleHit(HitInfo.From(collision));
        private void OnCollisionEnter2D(Collision2D collision) => HandleHit(HitInfo.From(collision));
        private void OnTriggerEnter(Collider other) => HandleHit(HitInfo.From(other, transform.position));
        private void OnTriggerEnter2D(Collider2D other) => HandleHit(HitInfo.From(other, transform.position));

        private void HandleHit(HitInfo hit)
        {
            if (IsDisposed || !enabled) return;
            Entity target = hit.Other.GetComponentInParent<Entity>();
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
            SetVelocity(Vector3.zero);
        }

        private IEnumerator DespawnNextFrame()
        {
            yield return null;
            Despawn();
        }

        private void SetVelocity(Vector3 velocity)
        {
            if (m_rigidbody3D) m_rigidbody3D.linearVelocity = velocity;
            else if (m_rigidbody2D) m_rigidbody2D.linearVelocity = velocity;
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
            Direction = Vector3.zero;
            Weapon = null;
            Owner = null;
            transform.position = new Vector3(10000, 10000, 10000);
        }

        #endregion

    }
}
