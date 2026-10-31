using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public class Movement : EntitySystem, IMovement
    {
        [SerializeField] private StatDefinition m_movementSpeedStatDefinition;
        [SerializeField] private float m_turnSpeed = 500;
        [SerializeField] private float m_baseSpeed = 5;
        [Tooltip("Faces the aim direction while aiming, otherwise the movement direction")]
        [SerializeField] private bool m_rotateToAimDirection;
        [Tooltip("2D body, found automatically when empty. Use either this or the 3D body")]
        [SerializeField] protected Rigidbody2D m_rigidbody;
        [Tooltip("3D body, found automatically when empty. Use either this or the 2D body")]
        [SerializeField] protected Rigidbody m_rigidbody3D;
        [SerializeField] private MovementFootsteps m_footsteps = new();

        public Vector3 MovementDirection { get; protected set; }
        public bool IsMoving => MovementDirection.magnitude > 1E-10;
        public virtual bool IsInCover => false;
        public virtual bool IsExposed => false;
        public float TraveledDistance { get; private set; }

        public Vector3 Position => Body.Position;
        public Vector3 Velocity => Body.Velocity;
        public float Speed => (m_baseSpeed + m_baseSpeed * (m_movementSpeedStat?.FinalFloatValue ?? 0) / 100f) * (1 - WeaponWeight);

        /// <summary>Speed fraction lost to the weight of the weapon being aimed.</summary>
        private float WeaponWeight => Entity && Entity.WeaponsSystem is { IsAiming: true, CurrentWeapon: { } weapon } ? weapon.WeaponData.weight : 0;

        /// <summary>The 2D or 3D body this movement drives, custom movements should only move through it.</summary>
        protected IBody Body { get; private set; }

        private bool m_hasExternalVelocity;
        private Stat m_movementSpeedStat;
        
        protected virtual void Awake()
        {
            Body = BodyFactory.Create(m_rigidbody, m_rigidbody3D) ?? BodyFactory.Find(gameObject);
        }

        protected virtual void Start()
        {
            if(m_movementSpeedStatDefinition)
                m_movementSpeedStat = Entity.StatsStore.GetOrCreateStat(m_movementSpeedStatDefinition);
        }

        /// <summary>An inactive movement stops driving the body, leaving it to physics (e.g. knockbacks while stunned).</summary>
        protected virtual void Update()
        {
            if (!IsSystemActive) return;
            UpdateVelocity();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsSystemActive) return;
            AdjustRotation();
        }

        protected virtual void LateUpdate()
        {
            bool canStep = IsSystemActive && !m_hasExternalVelocity && !(Entity && Entity.Health is { IsDead: true });
            m_footsteps?.Update(Body, canStep, IsInCover, Time.deltaTime);
        }
        
        public virtual void Move(Vector3 direction) 
        {
            SetMovementForce(direction);
        }

        public virtual void TakeCover() => throw new System.NotImplementedException();

        public virtual void LeaveCover() => throw new System.NotImplementedException();

        public virtual void SetCoverExposure(bool exposed, Vector3 aimDirection) => throw new System.NotImplementedException();

        public Vector3 ToWorld(Vector2 planar) => Body.ToWorld(planar);

        
        protected virtual void SetMovementForce(Vector3 force)
        {
            if (!IsSystemActive) return;
            MovementDirection = Body.Flatten(force).normalized;
        }
        
        protected virtual void AdjustRotation()
        {
            if (!m_rotateToAimDirection) return;
            IWeaponsSystem weaponsSystem = Entity.WeaponsSystem;
            bool isAiming = weaponsSystem is { IsAiming: true };
            if (!isAiming && !IsMoving) return;

            Vector3 direction = Body.Flatten(isAiming ? weaponsSystem.AimDirection : MovementDirection);
            if (direction.sqrMagnitude < 1E-10) return;
            Body.RotateTowards(direction, m_turnSpeed * Time.fixedDeltaTime);
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
            m_footsteps?.Reset();
        }

        /// <summary>Walks on the movement plane while retaining the system's distance metric.</summary>
        protected void ApplyVelocity(Vector3 velocity, float deltaTime)
        {
            Body.SetPlanarVelocity(velocity);
            TraveledDistance += velocity.magnitude * deltaTime;
        }
    }
}
