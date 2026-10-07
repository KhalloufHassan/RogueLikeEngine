using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public class Movement : EntitySystem, IMovement
    {
        [SerializeField] private StatDefinition m_movementSpeedStatDefinition;
        [SerializeField] private float m_turnSpeed = 500;
        [SerializeField] private float m_baseSpeed = 5;
        [SerializeField] private bool m_rotateToAimDirection;
        [Tooltip("2D body, found automatically when empty. Use either this or the 3D body")]
        [SerializeField] protected Rigidbody2D m_rigidbody;
        [Tooltip("3D body, found automatically when empty. Use either this or the 2D body")]
        [SerializeField] protected Rigidbody m_rigidbody3D;

        public Vector3 MovementDirection { get; protected set; }
        public bool IsMoving => MovementDirection.magnitude > 1E-10;
        public float TraveledDistance { get; private set; }

        public Vector3 Position => Body.Position;
        public Vector3 Velocity => Body.Velocity;
        public float Speed => m_baseSpeed + m_baseSpeed * (m_movementSpeedStat?.FinalFloatValue ?? 0) / 100f;

        /// <summary>The 2D or 3D body this movement drives, custom movements should only move through it.</summary>
        protected IBody Body { get; private set; }

        private bool m_hasExternalVelocity;
        private Stat m_movementSpeedStat;
        
        protected virtual void Awake()
        {
            Body = BodyFactory.Create(m_rigidbody, m_rigidbody3D) ?? BodyFactory.Find(gameObject);
        }

        private void Start()
        {
            if(m_movementSpeedStatDefinition)
                m_movementSpeedStat = Entity.StatsStore.GetOrCreateStat(m_movementSpeedStatDefinition);
            m_rotateToAimDirection = m_rotateToAimDirection && Entity.WeaponsSystem != null;
        }

        protected virtual void Update()
        {
            if(!IsSystemActive) 
                MovementDirection = Vector3.zero;
            UpdateVelocity();
            AdjustRotation();
        }
        
        public virtual void Move(Vector3 direction) 
        {
            SetMovementForce(direction);
        }

        public Vector3 ToWorld(Vector2 planar) => Body.ToWorld(planar);

        
        protected virtual void SetMovementForce(Vector3 force)
        {
            if (!IsSystemActive) return;
            MovementDirection = Body.Flatten(force).normalized;
        }
        
        protected virtual void AdjustRotation()
        {
            if (m_rotateToAimDirection)
            {
                if (Entity.WeaponsSystem.IsAiming || IsMoving)
                {
                    Vector3 direction = Body.Flatten(Entity.WeaponsSystem.IsAiming ? Entity.WeaponsSystem.AimDirection : MovementDirection);
                    if (direction.sqrMagnitude < 1E-10) return;
                    Quaternion target = Body.FacingRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, m_turnSpeed * Time.deltaTime);
                }
            }
        }

        protected virtual void UpdateVelocity()
        {
            if (m_hasExternalVelocity) return;
            ApplyVelocity(MovementDirection * Speed, Time.deltaTime);
        }

        /// <summary>Applies externally driven motion, suppressing walking until cleared. Call from the physics loop.</summary>
        public void SetExternalVelocity(Vector3 velocity, float deltaTime)
        {
            if (!IsSystemActive || !isActiveAndEnabled) return;
            m_hasExternalVelocity = true;
            Body.SetVelocity(velocity);
            TraveledDistance += velocity.magnitude * deltaTime;
        }

        /// <summary>Resets the distance metric, e.g. when a pooled entity is reused.</summary>
        public void ResetTraveledDistance() => TraveledDistance = 0;

        /// <summary>Stops externally driven motion and allows walking to resume.</summary>
        public void ClearExternalVelocity()
        {
            m_hasExternalVelocity = false;
            Body?.SetPlanarVelocity(Vector3.zero);
        }

        protected override void OnSystemActiveChanged()
        {
            if (!IsSystemActive)
            {
                MovementDirection = Vector3.zero;
                ClearExternalVelocity();
            }
        }

        protected virtual void OnDisable()
        {
            MovementDirection = Vector3.zero;
            ClearExternalVelocity();
        }

        /// <summary>Walks on the movement plane while retaining the system's distance metric.</summary>
        protected void ApplyVelocity(Vector3 velocity, float deltaTime)
        {
            Body.SetPlanarVelocity(velocity);
            TraveledDistance += velocity.magnitude * deltaTime;
        }
    }
}
