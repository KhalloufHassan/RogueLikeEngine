using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public class Movement : EntitySystem
    {
        [SerializeField] private StatDefinition m_movementSpeedStatDefinition;
        [SerializeField] private float m_turnSpeed = 500;
        [SerializeField] private float m_baseSpeed = 5;
        [SerializeField] private bool m_rotateToAimDirection;
        [SerializeField] protected Rigidbody2D m_rigidbody;

        public Vector2 MovementDirection { get; protected set; }
        public bool IsMoving => MovementDirection.magnitude > 1E-10;
        public float TraveledDistance { get; private set; }

        public Vector2 Position => m_rigidbody.position;
        public Vector2 Velocity => m_rigidbody.linearVelocity;
        public float Speed => m_baseSpeed + m_baseSpeed * (m_movementSpeedStat?.FinalFloatValue ?? 0) / 100f;

        private bool m_hasExternalVelocity;
        private Stat m_movementSpeedStat;
        
        private void Start()
        {
            if(m_movementSpeedStatDefinition)
                m_movementSpeedStat = Entity.StatsStore.GetOrCreateStat(m_movementSpeedStatDefinition);
            m_rotateToAimDirection = m_rotateToAimDirection && Entity.WeaponsSystem;
        }

        protected virtual void Update()
        {
            if(!IsSystemActive) 
                MovementDirection = Vector3.zero;
            UpdateVelocity();
            AdjustRotation();
        }
        
        public virtual void Move(Vector2 direction) 
        {
            SetMovementForce(direction);
        }

        
        protected virtual void SetMovementForce(Vector2 force)
        {
            if (!IsSystemActive) return;
            MovementDirection = force.normalized;
        }
        
        protected virtual void AdjustRotation()
        {
            if (m_rotateToAimDirection)
            {
                if (Entity.WeaponsSystem.IsAiming || IsMoving)
                {
                    Vector2 direction = Entity.WeaponsSystem.IsAiming ? Entity.WeaponsSystem.AimDirection : MovementDirection;
                    float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg -90f;
                    float currentAngle = transform.eulerAngles.z;
                    float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, m_turnSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(0, 0, newAngle);
                }
            }
        }
        
        protected virtual void UpdateVelocity()
        {
            if (m_hasExternalVelocity) return;
            ApplyVelocity(MovementDirection * Speed, Time.deltaTime);
        }

        /// <summary>Applies externally driven motion, suppressing walking until cleared. Call from the physics loop.</summary>
        public void SetExternalVelocity(Vector2 velocity, float deltaTime)
        {
            if (!IsSystemActive || !isActiveAndEnabled) return;
            m_hasExternalVelocity = true;
            ApplyVelocity(velocity, deltaTime);
        }

        /// <summary>Resets the distance metric, e.g. when a pooled entity is reused.</summary>
        public void ResetTraveledDistance() => TraveledDistance = 0;

        /// <summary>Stops externally driven motion and allows walking to resume.</summary>
        public void ClearExternalVelocity()
        {
            m_hasExternalVelocity = false;
            if (m_rigidbody) ApplyVelocity(Vector2.zero, 0);
        }

        protected override void OnSystemActiveChanged()
        {
            if (!IsSystemActive)
            {
                MovementDirection = Vector2.zero;
                ClearExternalVelocity();
            }
        }

        protected virtual void OnDisable()
        {
            MovementDirection = Vector2.zero;
            ClearExternalVelocity();
        }

        /// <summary>Applies movement while retaining the system's distance metric.</summary>
        protected void ApplyVelocity(Vector2 velocity, float deltaTime)
        {
            m_rigidbody.linearVelocity = velocity;
            TraveledDistance += velocity.magnitude * deltaTime;
        }
    }
}
