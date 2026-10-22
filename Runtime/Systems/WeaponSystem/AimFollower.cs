using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    /// <summary>Default aim presentation: orbits the weapons system at a fixed distance and faces its aim direction.</summary>
    public class AimFollower : MonoBehaviour
    {
        [SerializeField] private WeaponsSystem m_weaponsSystem;
        [Tooltip("Distance from the weapons system's position, along the aim direction")]
        [SerializeField] private float m_distanceFromOwner = 0.75f;

        private Vector3 m_startScale;
        private bool m_is3D;
        private float m_heightOffset;
        private Vector3 m_lastAimDirection;

        private void Start()
        {
            m_startScale = transform.localScale;
            m_is3D = m_weaponsSystem.GetComponentInParent<Rigidbody>();
            m_heightOffset = transform.position.y - m_weaponsSystem.transform.position.y;
            m_lastAimDirection = Vector3.ProjectOnPlane(m_weaponsSystem.transform.forward, Vector3.up).normalized;
        }

        private void Update()
        {
            if (m_is3D)
                Follow3D();
            else
                Follow2D();
        }

        private void Follow3D()
        {
            Vector3 aim = Vector3.ProjectOnPlane(m_weaponsSystem.AimDirection, Vector3.up);
            if (aim.sqrMagnitude > 1E-10) m_lastAimDirection = aim.normalized;
            if (m_lastAimDirection.sqrMagnitude < 1E-10) return;

            transform.position = m_weaponsSystem.transform.position + m_lastAimDirection * m_distanceFromOwner + Vector3.up * m_heightOffset;
            transform.rotation = Quaternion.LookRotation(m_lastAimDirection);
        }

        private void Follow2D()
        {
            float angle = Mathf.Atan2(m_weaponsSystem.AimDirection.y, m_weaponsSystem.AimDirection.x);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * m_distanceFromOwner;
            transform.position = (Vector2)m_weaponsSystem.transform.position + offset;
            transform.right = offset;

            if (m_weaponsSystem.AimDirection.x < 0)
                transform.localScale = new Vector3(m_startScale.x, -m_startScale.y, m_startScale.z);
            else
                transform.localScale = m_startScale;
        }
    }
}
