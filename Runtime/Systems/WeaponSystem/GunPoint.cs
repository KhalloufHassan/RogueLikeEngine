using RogueLikeEngine.Utils.Timers;
using UnityEngine;

namespace RogueLikeEngine.Systems.Weapons
{
    public class GunPoint : MonoBehaviour
    {
        [SerializeField] private WeaponsSystem weaponsSystem;
        [SerializeField] private Transform shootingTransform;
        [SerializeField] private WeaponData weaponData;
        [Tooltip("Distance from the weapons system's position, along the aim direction")]
        [SerializeField] private float m_distanceFromOwner = 0.75f;

        public WeaponInstance CurrentWeapon { get; private set; }
        public bool CanFire => !IsOnCooldown;
        public bool IsOnCooldown => !m_coolDownTimer.IsFinished;

        private AutoTimer m_coolDownTimer;

        private Vector3 m_startScale;
        private bool m_is3D;
        private float m_heightOffset;
        private Vector3 m_lastAimDirection;

        private void Awake()
        {
            CurrentWeapon = weaponData.CreateWeaponInstance();
            m_startScale = transform.localScale;
        }

        private void Start()
        {
            m_is3D = weaponsSystem.GetComponentInParent<Rigidbody>();
            m_heightOffset = transform.position.y - weaponsSystem.transform.position.y;
            m_lastAimDirection = Vector3.ProjectOnPlane(weaponsSystem.transform.forward, Vector3.up).normalized;
        }

        private void Update()
        {
            if (m_is3D)
                FollowAim3D();
            else
                FollowAim2D();
        }

        private void FollowAim3D()
        {
            Vector3 aim = Vector3.ProjectOnPlane(weaponsSystem.AimDirection, Vector3.up);
            if (aim.sqrMagnitude > 1E-10) m_lastAimDirection = aim.normalized;
            if (m_lastAimDirection.sqrMagnitude < 1E-10) return;

            transform.position = weaponsSystem.transform.position + m_lastAimDirection * m_distanceFromOwner + Vector3.up * m_heightOffset;
            transform.rotation = Quaternion.LookRotation(m_lastAimDirection);
        }

        private void FollowAim2D()
        {
            float angle = Mathf.Atan2(weaponsSystem.AimDirection.y, weaponsSystem.AimDirection.x);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * m_distanceFromOwner;
            transform.position = (Vector2)weaponsSystem.transform.position + offset;
            transform.right = offset;
            
            if(weaponsSystem.AimDirection.x < 0)
                transform.localScale = new Vector3(m_startScale.x,-m_startScale.y,m_startScale.z);
            else
                transform.localScale = m_startScale;
        }

        public void Fire()
        {
            Projectile projectile = CurrentWeapon.CreateProjectile(weaponsSystem);
            projectile.transform.position = shootingTransform.position;
            projectile.SetDirection(m_is3D ? shootingTransform.forward : shootingTransform.right);

            m_coolDownTimer = 1f / weaponsSystem.CalculatedFireRate(CurrentWeapon.WeaponData);
            m_coolDownTimer.Reset();
        }
    }
}