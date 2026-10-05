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
        
        private void Awake()
        {
            CurrentWeapon = weaponData.CreateWeaponInstance();
            m_startScale = transform.localScale;
        }
        
        private void Update()
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
            projectile.SetDirection(shootingTransform.right);

            m_coolDownTimer = 1f / weaponsSystem.CalculatedFireRate(CurrentWeapon.WeaponData);
            m_coolDownTimer.Reset();
        }
    }
}