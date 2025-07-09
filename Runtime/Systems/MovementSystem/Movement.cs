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

        private Stat m_movementSpeedStat;
        
        private void Start()
        {
            if(m_movementSpeedStatDefinition)
                m_movementSpeedStat = Entity.StatsStore.GetOrCreateStat(m_movementSpeedStatDefinition);
            m_rotateToAimDirection = m_rotateToAimDirection && Entity.WeaponsSystem;
        }

        protected void Update()
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
        
        private void UpdateVelocity()
        {
            float speed = m_baseSpeed;
            if(m_movementSpeedStat != null)
                speed += m_baseSpeed * (m_movementSpeedStat.FinalValue/100f);
            m_rigidbody.linearVelocity = MovementDirection * speed;
            
            TraveledDistance += (MovementDirection * speed * Time.deltaTime).magnitude;
        }
    }
}