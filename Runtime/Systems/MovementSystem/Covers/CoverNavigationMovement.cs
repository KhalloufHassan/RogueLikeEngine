using UnityEngine;
using UnityEngine.AI;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>Moves toward world destinations using navigation paths while retaining cover movement.</summary>
    public class CoverNavigationMovement : CoverMovement
    {
        [SerializeField] private int m_agentTypeID;
        [SerializeField] private float m_repathInterval = .35f;
        [SerializeField] private float m_arrivalDistance = .15f;

        private readonly Vector3[] m_corners = new Vector3[64];
        private NavMeshPath m_path;
        private Vector3 m_destination;
        private bool m_hasDestination;
        private float m_repathTime;
        private int m_corner;
        private int m_cornerCount;

        protected override void Awake()
        {
            base.Awake();
            m_path = new NavMeshPath();
        }

        public override void Move(Vector3 destination)
        {
            if (!IsSystemActive || !isActiveAndEnabled) return;
            if ((destination - m_destination).sqrMagnitude > .25f || !m_hasDestination) m_repathTime = 0;
            m_destination = destination;
            m_hasDestination = Vector3.ProjectOnPlane(destination - Position, Vector3.up).sqrMagnitude > m_arrivalDistance * m_arrivalDistance;
            if (!m_hasDestination) base.Move(Vector3.zero);
        }

        protected override void Update()
        {
            if (IsSystemActive && m_hasDestination)
            {
                Vector3 offset = Vector3.ProjectOnPlane(m_destination - Position, Vector3.up);
                if (offset.sqrMagnitude <= m_arrivalDistance * m_arrivalDistance)
                {
                    m_hasDestination = false;
                    base.Move(Vector3.zero);
                }
                else if (IsInCover)
                    base.Move(offset);
                else
                {
                    m_repathTime -= Time.deltaTime;
                    if (m_repathTime <= 0) UpdatePath();
                    while (m_corner < m_cornerCount && Vector3.ProjectOnPlane(m_corners[m_corner] - Position, Vector3.up).sqrMagnitude < .04f)
                        m_corner++;
                    base.Move(m_corner < m_cornerCount ? m_corners[m_corner] - Position : Vector3.zero);
                }
            }
            base.Update();
        }

        private void UpdatePath()
        {
            m_repathTime = m_repathInterval;
            m_corner = 0;
            m_cornerCount = 0;
            NavMeshQueryFilter filter = new() { agentTypeID = m_agentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(Position, out NavMeshHit start, 1, filter) ||
                !NavMesh.SamplePosition(m_destination, out NavMeshHit end, .5f, filter) ||
                !NavMesh.CalculatePath(start.position, end.position, filter, m_path) || m_path.status != NavMeshPathStatus.PathComplete) return;
            m_cornerCount = m_path.GetCornersNonAlloc(m_corners);
            if (m_cornerCount == m_corners.Length) m_cornerCount = 0;
        }

        protected override void OnSystemActiveChanged()
        {
            base.OnSystemActiveChanged();
            if (!IsSystemActive) m_hasDestination = false;
        }

        protected override void OnDisable()
        {
            m_hasDestination = false;
            base.OnDisable();
        }
    }
}
