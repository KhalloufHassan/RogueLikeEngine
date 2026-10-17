using RogueLikeEngine.Utils;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>Extends normal movement with high and low cover and movement along walls.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class CoverMovement : Movement
    {
        [SerializeField] private CapsuleCollider m_capsule;
        [SerializeField] private LayerMask m_coverLayers = Physics.AllLayers;
        [SerializeField] private float m_coverRange = 1.2f;
        [SerializeField] private float m_alignSpeed = 3;
        [SerializeField] private float m_crouchHeight = 1;
        [SerializeField] private float m_coverSpeedMultiplier = .5f;
        [SerializeField] private float m_transitionSpeedMultiplier = .3f;
        [SerializeField] private float m_transitionDuration = .5f;

        private CoverState m_coverState;
        private Collider m_wall;
        private Cover m_cover;
        private Vector3 m_coverPosition;
        private Vector3 m_coverNormal;
        private Vector3 m_input;
        private Vector3 m_standingCenter;
        private float m_standingHeight;
        private bool m_highCover;
        private float m_transitionTimeLeft;

        public Cover AvailableCover { get; private set; }

        public CoverState State => m_coverState;
        public bool IsHighCover => m_highCover;
        public Vector3 CoverNormal => m_coverNormal;
        public override bool IsInCover => m_coverState is CoverState.Entering or CoverState.Holding;
        public float CoverSpeed => Speed * (m_coverState == CoverState.Holding ? m_coverSpeedMultiplier : m_transitionSpeedMultiplier);

        protected override void Awake()
        {
            if (!m_capsule) m_capsule = GetComponent<CapsuleCollider>();
            if (!m_rigidbody3D) m_rigidbody3D = GetComponent<Rigidbody>();
            base.Awake();
            m_standingHeight = m_capsule.height;
            m_standingCenter = m_capsule.center;
        }

        public override void Move(Vector3 direction)
        {
            m_input = IsSystemActive ? direction : Vector3.zero;
            base.Move(IsInCover ? Vector3.zero : m_input);
        }

        public override void TakeCover()
        {
            if (!IsSystemActive || !isActiveAndEnabled || IsInCover) return;
            TryEnterCover();
        }

        public override void LeaveCover()
        {
            if (!IsSystemActive || !isActiveAndEnabled || !IsInCover || !CanStand()) return;
            m_coverState = CoverState.Leaving;
            m_transitionTimeLeft = m_transitionDuration;
            RestoreMovement();
        }

        protected override void Update()
        {
            base.Update();
            Collider candidate = IsSystemActive && !IsInCover ? FindCover().collider : null;
            SetAvailableCover(candidate ? candidate.GetComponentInParent<Cover>() : null);
            if (!IsSystemActive || m_coverState == CoverState.None) return;
            if (IsInCover && (!m_cover || !m_cover.isActiveAndEnabled || !m_wall || !m_wall.enabled || !m_wall.gameObject.activeInHierarchy))
            {
                ResetCover();
                return;
            }

            if (m_coverState is not (CoverState.Entering or CoverState.Leaving)) return;
            m_transitionTimeLeft -= Time.deltaTime;
            if (m_transitionTimeLeft > 0) return;
            if (m_coverState == CoverState.Entering)
                m_coverState = CoverState.Holding;
            else
                ResetCover();
        }

        protected override void UpdateVelocity()
        {
            if (m_coverState == CoverState.Leaving)
                ApplyVelocity(MovementDirection * CoverSpeed, Time.deltaTime);
            else if (!IsInCover)
                base.UpdateVelocity();
        }

        protected override void FixedUpdate()
        {
            if (!IsInCover)
            {
                base.FixedUpdate();
                return;
            }
            if (!IsSystemActive) return;

            MoveAlongCover();
            Vector3 offset = Body.Flatten(m_coverPosition - Position);
            Vector3 alignment = Vector3.Project(offset, m_coverNormal);
            Vector3 velocity = Vector3.ClampMagnitude(alignment / Time.fixedDeltaTime, m_alignSpeed)
                + Vector3.ClampMagnitude((offset - alignment) / Time.fixedDeltaTime, CoverSpeed);
            if (m_coverState == CoverState.Entering) velocity = Vector3.ClampMagnitude(velocity, CoverSpeed);
            ApplyVelocity(velocity, Time.fixedDeltaTime);
            Body.RotateTowards(m_coverNormal, 720 * Time.fixedDeltaTime);
        }

        private void MoveAlongCover()
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, m_coverNormal);
            m_coverPosition = Position + Vector3.Project(m_coverPosition - Position, m_coverNormal);
            if (m_input.sqrMagnitude < 1E-6f) return;

            Vector3 input = m_input.normalized;
            float alongWall = Vector3.Dot(input, tangent);
            if (Mathf.Abs(alongWall) < 1E-6f)
            {
                float inputAxis = Mathf.Abs(input.x) >= Mathf.Abs(input.z) ? input.x : input.z;
                float wallAxis = Mathf.Abs(tangent.x) >= Mathf.Abs(tangent.z) ? tangent.x : tangent.z;
                alongWall = inputAxis * wallAxis;
            }

            Vector3 direction = tangent * Mathf.Sign(alongWall);
            float travel = CoverSpeed * Time.fixedDeltaTime;
            foreach (RaycastHit obstacle in m_rigidbody3D.SweepTestAll(direction, travel + .02f, QueryTriggerInteraction.Ignore))
                if (Vector3.Dot(obstacle.normal, direction) < -.01f)
                    travel = Mathf.Min(travel, Mathf.Max(0, obstacle.distance - .02f));
            if (travel < 1E-6f) return;

            Vector3 leadingEdge = direction * m_capsule.radius;
            Vector3 origin = m_coverPosition + direction * travel + leadingEdge + Vector3.up * .6f;
            if (!Physics.Raycast(origin, -m_coverNormal, out RaycastHit wall, m_capsule.radius + .15f, m_coverLayers, QueryTriggerInteraction.Ignore)) return;
            if (wall.collider is CapsuleCollider) return;
            if (Vector3.Dot(wall.normal, m_coverNormal) < .99f) return;
            Cover cover = wall.collider.GetComponentInParent<Cover>();
            if (!cover || !cover.isActiveAndEnabled || cover.Type != m_cover.Type) return;

            m_wall = wall.collider;
            m_cover = cover;
            m_coverPosition = wall.point - leadingEdge + m_coverNormal * (m_capsule.radius + .05f);
            m_coverPosition.y = Position.y;
        }

        private RaycastHit FindCover()
        {
            Vector3 origin = Position + Vector3.up * .6f;
            float nearest = m_coverRange;
            RaycastHit cover = default;
            foreach (Collider wall in PhysicsUtils.OverlapSphere(origin, m_coverRange, m_coverLayers, QueryTriggerInteraction.Ignore))
            {
                if (wall is CapsuleCollider) continue;
                Cover candidate = wall.GetComponentInParent<Cover>();
                if (!candidate || !candidate.isActiveAndEnabled) continue;
                Vector3 direction = Body.Flatten(wall.ClosestPoint(origin) - origin);
                if (direction.sqrMagnitude < 1E-6f) continue;
                if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, nearest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider != wall || Mathf.Abs(hit.normal.y) > .1f) continue;
                nearest = hit.distance;
                cover = hit;
            }
            return cover;
        }

        private void SetAvailableCover(Cover cover)
        {
            if (AvailableCover == cover) return;
            if (AvailableCover) AvailableCover.IsAvailable = false;
            AvailableCover = cover;
            if (AvailableCover) AvailableCover.IsAvailable = true;
        }

        private void TryEnterCover()
        {
            RaycastHit cover = FindCover();
            if (!cover.collider) return;

            SetAvailableCover(null);
            m_wall = cover.collider;
            m_cover = m_wall.GetComponentInParent<Cover>();
            m_highCover = m_cover.Type == CoverType.High;
            m_coverNormal = Body.Flatten(cover.normal).normalized;
            m_coverPosition = cover.point + m_coverNormal * (m_capsule.radius + .05f);
            m_coverPosition.y = Position.y;
            m_coverState = CoverState.Entering;
            m_transitionTimeLeft = m_transitionDuration;
            base.Move(Vector3.zero);
            if (!m_highCover)
            {
                m_capsule.height = m_crouchHeight;
                m_capsule.center = m_standingCenter - Vector3.up * ((m_standingHeight - m_crouchHeight) / 2);
            }
        }

        private bool CanStand()
        {
            if (m_highCover) return true;
            Vector3 center = transform.TransformPoint(m_standingCenter);
            float radius = m_capsule.radius - .01f;
            Vector3 offset = Vector3.up * (m_standingHeight / 2 - m_capsule.radius);
            foreach (Collider obstacle in PhysicsUtils.OverlapCapsule(center - offset, center + offset, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (obstacle.attachedRigidbody != m_rigidbody3D) return false;
            return true;
        }

        private void ResetCover()
        {
            if (m_coverState == CoverState.None) return;
            m_coverState = CoverState.None;
            RestoreMovement();
        }

        private void RestoreMovement()
        {
            m_cover = null;
            m_wall = null;
            m_capsule.height = m_standingHeight;
            m_capsule.center = m_standingCenter;
            base.Move(m_input);
        }

        protected override void OnSystemActiveChanged()
        {
            base.OnSystemActiveChanged();
            if (IsSystemActive) return;
            SetAvailableCover(null);
            m_input = Vector3.zero;
            ResetCover();
        }

        protected override void OnDisable()
        {
            SetAvailableCover(null);
            m_input = Vector3.zero;
            ResetCover();
            base.OnDisable();
        }
    }
}
