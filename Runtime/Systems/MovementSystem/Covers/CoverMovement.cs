using RogueLikeEngine.Utils;
using RogueLikeEngine.Systems.Weapons;
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
        [SerializeField, Min(0)] private float m_exitDuration = .3f;
        [Tooltip("Lateral reach used to check an upper-body peek without moving the body")]
        [SerializeField, Min(0)] private float m_exposureDistance = .55f;
        [Tooltip("Seconds to lean out of cover once clear, before IsExposed allows shooting")]
        [SerializeField, Min(0)] private float m_exposeDuration;

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
        private bool m_exposureRequested;
        private bool m_isExposed;
        private Vector3 m_exposureAim;
        private Vector3 m_requestedAim;

        public Cover AvailableCover { get; private set; }

        public CoverState State => m_coverState;
        public bool IsHighCover => m_highCover;
        public Vector3 CoverNormal => m_coverNormal;
        public override bool IsInCover => m_coverState is CoverState.Entering or CoverState.Holding;
        public override bool IsExposed => m_isExposed;
        public Vector3 ExposureDirection { get; private set; }
        /// <summary>Progress of leaning out of cover from 0 to 1, IsExposed once it reaches 1. Resets to 0 when hiding.</summary>
        public float ExposureProgress { get; private set; }
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
            m_transitionTimeLeft = m_exitDuration;
            RestoreMovement();
        }

        /// <summary>Requests exposure while in cover. Physics clearance determines IsExposed.</summary>
        public override void SetCoverExposure(bool exposed, Vector3 aimDirection)
        {
            m_exposureRequested = exposed && IsInCover && IsSystemActive && isActiveAndEnabled;
            if (!m_exposureRequested)
            {
                Hide();
                return;
            }
            m_requestedAim = Body.Flatten(aimDirection).normalized;
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
            UpdateExposure();
            Vector3 offset = Body.Flatten(m_coverPosition - Position);
            Vector3 alignment = Vector3.Project(offset, m_coverNormal);
            Vector3 velocity = Vector3.ClampMagnitude(alignment / Time.fixedDeltaTime, m_alignSpeed)
                + Vector3.ClampMagnitude((offset - alignment) / Time.fixedDeltaTime, CoverSpeed);
            if (m_coverState == CoverState.Entering) velocity = Vector3.ClampMagnitude(velocity, CoverSpeed);
            ApplyVelocity(velocity, Time.fixedDeltaTime);
            Body.RotateTowards(ExposureProgress > 0 && !m_highCover ? m_exposureAim : m_coverNormal, 720 * Time.fixedDeltaTime);
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

        private void UpdateExposure()
        {
            IWeaponsSystem weapons = Entity ? Entity.WeaponsSystem : null;
            bool firing = weapons is { IsFireHeld: true };
            m_exposureAim = m_exposureRequested ? m_requestedAim : firing
                ? Body.Flatten(weapons.IsAiming ? weapons.AimDirection : transform.forward).normalized
                : Vector3.zero;
            if ((!m_exposureRequested && !firing) || m_coverState != CoverState.Holding || m_exposureAim.sqrMagnitude < 1E-6f)
            {
                Hide();
                return;
            }

            if (!m_highCover)
            {
                if (CanStand())
                {
                    SetCapsuleHeight(m_standingHeight);
                    LeanOut();
                }
                else
                    Hide();
                return;
            }

            Vector3 tangent = Vector3.Cross(Vector3.up, m_coverNormal);
            float side = Vector3.Dot(m_exposureAim, tangent);
            if (Mathf.Abs(side) < .01f && ExposureProgress > 0) side = Vector3.Dot(ExposureDirection, tangent);
            Vector3 preferred = tangent * (side < 0 ? -m_exposureDistance : m_exposureDistance);
            if (!CanExposeAt(preferred))
            {
                preferred = -preferred;
                if (!CanExposeAt(preferred))
                {
                    Hide();
                    return;
                }
            }

            ExposureDirection = preferred.normalized;
            LeanOut();
        }

        private void LeanOut()
        {
            ExposureProgress = m_exposeDuration > 0 ? Mathf.MoveTowards(ExposureProgress, 1, Time.fixedDeltaTime / m_exposeDuration) : 1;
            m_isExposed = ExposureProgress >= 1;
        }

        private bool CanExposeAt(Vector3 offset)
        {
            Vector3 position = m_coverPosition + offset;
            Vector3 innerEdge = position - offset.normalized * .1f + Vector3.up * .6f;
            if (!HasClearLine(innerEdge, innerEdge - m_coverNormal * (m_capsule.radius + .15f))) return false;

            Vector3 center = m_coverPosition + offset * .5f + Vector3.up * (m_standingHeight * .8f);
            Vector3 halfSize = new(offset.magnitude * .5f + .15f, .2f, .15f);
            foreach (Collider obstacle in PhysicsUtils.OverlapBox(center, halfSize, Quaternion.LookRotation(m_coverNormal),
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (obstacle.attachedRigidbody != m_rigidbody3D) return false;

            Vector3 origin = position + Vector3.up * (m_standingHeight * .8f);
            return HasClearLine(origin, origin + m_exposureAim * (m_capsule.radius + .4f));
        }

        private bool HasClearLine(Vector3 start, Vector3 end)
        {
            foreach (Collider obstacle in PhysicsUtils.OverlapCapsule(start, end, .01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (obstacle.attachedRigidbody != m_rigidbody3D) return false;
            return true;
        }

        private void Hide()
        {
            m_isExposed = false;
            ExposureProgress = 0;
            ExposureDirection = Vector3.zero;
            if (IsInCover && !m_highCover) SetCapsuleHeight(m_crouchHeight);
        }

        private void SetCapsuleHeight(float height)
        {
            m_capsule.height = height;
            m_capsule.center = m_standingCenter - Vector3.up * ((m_standingHeight - height) / 2);
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
                SetCapsuleHeight(m_crouchHeight);
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
            m_exposureRequested = false;
            Hide();
            m_cover = null;
            m_wall = null;
            SetCapsuleHeight(m_standingHeight);
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
