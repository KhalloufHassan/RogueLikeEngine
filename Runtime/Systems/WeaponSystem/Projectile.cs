using System;
using System.Collections;
using System.Collections.Generic;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Surfaces;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Healths;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class Projectile : MonoBehaviour,IPoolObject
    {
        [Tooltip("Optional sound and VFX played where the projectile hits, picked by the hit object's Surface")]
        [SerializeField] private SurfaceEffects m_impactEffects;

        public WeaponInstance Weapon { get; set; }
        public Damage Damage { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public Vector3 Direction { get; private set; }
        public float TraveledDistance { get; private set; }

        public Entity Owner { get; set; }

        /// <summary>Optional Entity on the same object, hosting the projectile's effects.</summary>
        public Entity Entity { get; private set; }

        public event Action OnLaunched;
        public event Action<HitInfo> OnHit;

        private Rigidbody2D m_rigidbody2D;
        private Rigidbody m_rigidbody3D;
        private Collider[] m_colliders3D;
        private Collider2D[] m_colliders2D;
        private readonly HashSet<Component> m_ignoredColliders = new();

        private void Awake()
        {
            m_rigidbody2D = GetComponent<Rigidbody2D>();
            m_rigidbody3D = GetComponent<Rigidbody>();
            Entity = GetComponent<Entity>();
        }

        public void SetDirection(Vector3 direction) => Direction = direction.normalized;

        public void Launch(Vector3 position, Vector3 direction)
        {
            ResetIgnoreCollisions();
            transform.position = position;
            SetDirection(direction);
            OnLaunched?.Invoke();
        }

        /// <summary>Passes through a collider for this flight. Restored on pool return or the next launch; supports 2D and 3D.</summary>
        public void IgnoreCollision(Component other)
        {
            if (!other || !m_ignoredColliders.Add(other)) return;
            SetIgnoreCollision(other, true);
            SetVelocity(Direction * Speed);
        }

        private void ResetIgnoreCollisions()
        {
            foreach (Component other in m_ignoredColliders)
                if (other) SetIgnoreCollision(other, false);
            m_ignoredColliders.Clear();
        }

        private void SetIgnoreCollision(Component other, bool ignore)
        {
            if (other is Collider collider)
            {
                m_colliders3D ??= GetComponentsInChildren<Collider>(true);
                foreach (Collider self in m_colliders3D)
                    if (self && self != collider) Physics.IgnoreCollision(self, collider, ignore);
            }
            else if (other is Collider2D collider2D)
            {
                m_colliders2D ??= GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D self in m_colliders2D)
                    if (self && self != collider2D) Physics2D.IgnoreCollision(self, collider2D, ignore);
            }
        }

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
            if (Owner && hit.Other.GetComponentInParent<Entity>() == Owner)
            {
                IgnoreCollision(hit.Collider);
                return;
            }
            Entity target = hit.Other.GetComponentInParent<Entity>();
            IHealth health = target && target != Owner ? target.Health : null;
            IProjectileAffector affector = hit.Collider.GetComponentInParent<IProjectileAffector>();
            AffectorResult result = default;
            if (affector != null && (affector is not Behaviour behaviour || behaviour.isActiveAndEnabled))
            {
                result = affector.OnProjectileHit(hit, this);
                if (!this || IsDisposed || !isActiveAndEnabled) return;
            }
            if (health != null && !result.IgnoreDamage)
            {
                health.TakeDamage(new Damage(Damage.Value, Direction));
                if (Weapon != null) Weapon.DamageDealt += Damage.Value;
            }
            if (result.IgnoreCollision || !this || IsDisposed || !isActiveAndEnabled) return;
            if (m_impactEffects) m_impactEffects.Play(Surface.Of(hit.Collider), hit.Point, hit.Normal, Direction);
            OnHit?.Invoke(hit);
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
            ResetIgnoreCollisions();
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
