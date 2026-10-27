using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>
    /// Where shots leave the weapon and which way they go. Cycles through its points, one per shot,
    /// or uses its own transform when none are assigned.
    /// </summary>
    public class GunPoints : MonoBehaviour
    {
        [SerializeField] private Transform[] m_points = Array.Empty<Transform>();
        [Tooltip("Layers that block shots: a point inside or behind them shoots from their surface instead. Nothing disables the check")]
        [SerializeField] private LayerMask m_blockingLayers;
        [Tooltip("Where the blocking check starts, e.g. the character's chest. When empty, it starts Clearance Distance behind the point")]
        [SerializeField] private Transform m_clearanceOrigin;
        [SerializeField] private float m_clearanceDistance = 1.5f;

        /// <summary>The current point's position, moved back to the surface of anything on <c>m_blockingLayers</c> between it and the clearance origin.</summary>
        public Vector3 Position
        {
            get
            {
                Vector3 point = Current.position;
                if (m_blockingLayers == 0) return point;
                Vector3 origin = m_clearanceOrigin ? m_clearanceOrigin.position : point - Forward * m_clearanceDistance;
                if (m_is3D)
                    return Physics.Linecast(origin, point, out RaycastHit hit, m_blockingLayers, QueryTriggerInteraction.Ignore) ? hit.point : point;
                RaycastHit2D hit2D = Physics2D.Linecast(origin, point, m_blockingLayers);
                return hit2D ? new Vector3(hit2D.point.x, hit2D.point.y, point.z) : point;
            }
        }

        /// <summary>The point the next shot leaves from.</summary>
        public Transform Current => m_points.Length == 0 ? transform : m_points[m_index];
        private Vector3 Forward => m_is3D ? Current.forward : Current.right;

        private int m_index;
        private bool m_is3D;

        private void Awake() => m_is3D = GetComponentInParent<Rigidbody>();

        /// <summary>The current point's shooting direction, rotated by <paramref name="angle"/> degrees on the gameplay plane.</summary>
        public Vector3 GetDirection(float angle) => GetDirection(Forward, angle);

        /// <summary><paramref name="direction"/> rotated by <paramref name="angle"/> degrees on the gameplay plane.</summary>
        public Vector3 GetDirection(Vector3 direction, float angle) => Quaternion.AngleAxis(angle, m_is3D ? Vector3.up : Vector3.forward) * direction;

        /// <summary>Moves on to the next point for the following shot.</summary>
        public void Advance()
        {
            if (m_points.Length > 0) m_index = (m_index + 1) % m_points.Length;
        }

        /// <summary>
        /// Draws the points (current one yellow), the clearance check (grey, red up to the blocking surface when blocked),
        /// the point's forward (blue) and the direction the next shot flies (green, the aim while aiming).
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) m_is3D = GetComponentInParent<Rigidbody>();

            Gizmos.color = Color.grey;
            foreach (Transform point in m_points)
                if (point) Gizmos.DrawWireSphere(point.position, .04f);

            Transform current = Current;
            if (!current) return;
            Vector3 position = Position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(current.position, .06f);

            if (m_blockingLayers != 0)
            {
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(m_clearanceOrigin ? m_clearanceOrigin.position : current.position - Forward * m_clearanceDistance, position);
                if (position != current.position)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(position, current.position);
                    Gizmos.DrawWireSphere(position, .06f);
                }
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, Forward * .5f);

            IWeaponsSystem weaponsSystem = GetComponentInParent<IWeaponsSystem>();
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, (weaponsSystem is { IsAiming: true } ? weaponsSystem.AimDirection : Forward) * 3f);
        }
    }
}
