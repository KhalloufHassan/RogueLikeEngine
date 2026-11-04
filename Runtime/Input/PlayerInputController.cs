using RogueLikeEngine.Systems.Abilities;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Entity player;
        [SerializeField] private Ability ability;

        [Header("Mouse Aim Assist")]
        [SerializeField] private bool m_aimAssist;
        [SerializeField] private GunPoints m_gunPoints;
        [SerializeField] private LayerMask m_aimLayers;
        [SerializeField] private float m_aimHeight;
        [SerializeField] private Camera m_aimCamera;

        private Mouse m_mouse;
        private InputAction m_aimAction;
        private bool m_restoreUnaimed;

        private bool firing;
        private bool usingAbility;
        private bool inputInterrupted;

        public void OnMove(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || player.Movement == null) return;
            Vector2 movementInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
            player.Movement.Move(ToWorld(movementInput));
        }

        public void OnCover(InputAction.CallbackContext context)
        {
            if (!context.performed || inputInterrupted || !isActiveAndEnabled || !player || player.Movement == null) return;
            IMovement movement = player.Movement;
            if (movement.IsInCover)
                movement.LeaveCover();
            else
                movement.TakeCover();
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !isActiveAndEnabled || !player || player.WeaponsSystem == null) return;
            m_aimAction = context.action;
            m_mouse = context.canceled ? null : context.control.device as Mouse;
            if (context.canceled) 
                player.WeaponsSystem.StopAim();
            else
                player.WeaponsSystem.Aim(ToWorld(context.ReadValue<Vector2>()));
            RefreshAim();
        }

        private void Update() => RefreshAim();

        /// <summary>Refreshes optional mouse assistance, including after presentation moves the muzzle.</summary>
        public void RefreshAim()
        {
            if (!m_aimAssist || m_mouse == null || m_aimAction == null || !m_aimAction.enabled ||
                inputInterrupted || !isActiveAndEnabled || !player || player.WeaponsSystem == null) return;
            if (!m_aimCamera) m_aimCamera = Camera.main;
            if (!m_aimCamera) return;
            bool is3D = player.GetComponent<Rigidbody>();
            Vector3 normal = is3D ? Vector3.up : Vector3.forward;
            Ray ray = m_aimCamera.ScreenPointToRay(m_mouse.position.ReadValue());
            Plane plane = new(normal, player.transform.position + (is3D ? Vector3.up * m_aimHeight : Vector3.zero));
            if (!plane.Raycast(ray, out float distance)) return;
            Vector3 point = ray.GetPoint(distance);
            Entity target = null;
            Vector3 center = point;
            if (is3D)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, m_aimCamera.farClipPlane, m_aimLayers, QueryTriggerInteraction.Ignore))
                {
                    target = hit.collider.GetComponentInParent<Entity>();
                    center = hit.collider.bounds.center;
                }
            }
            else
            {
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, m_aimCamera.farClipPlane, m_aimLayers);
                if (hit.collider)
                {
                    target = hit.collider.GetComponentInParent<Entity>();
                    center = hit.collider.bounds.center;
                }
            }
            if (target && target != player && target.Health is { IsDead: false }) point = center;
            Vector3 origin = m_gunPoints ? m_gunPoints.Position : player.transform.position;
            Vector3 direction = Vector3.ProjectOnPlane(point - player.transform.position, normal);
            Vector3 muzzleOffset = Vector3.ProjectOnPlane(origin - player.transform.position, normal);
            float reach = muzzleOffset.magnitude;
            // A nearby target can sit behind the moving muzzle, making pose and aim repeatedly reverse each other.
            float correction = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(reach, reach * 3, direction.magnitude));
            direction -= muzzleOffset * correction;
            if (direction.sqrMagnitude > .000001f) player.WeaponsSystem.Aim(direction.normalized);
        }

        /// <summary>Keeps assisted shots on the gameplay plane, including firing with a neutral stick.</summary>
        public void PrepareShotAim()
        {
            if (!m_aimAssist || !player || player.WeaponsSystem == null) return;
            IWeaponsSystem weapons = player.WeaponsSystem;
            bool is3D = player.GetComponent<Rigidbody>();
            m_restoreUnaimed = !weapons.IsAiming;
            Vector3 facing = is3D ? player.transform.forward : player.transform.up;
            weapons.Aim(Vector3.ProjectOnPlane(m_restoreUnaimed ? facing : weapons.AimDirection, is3D ? Vector3.up : Vector3.forward).normalized);
        }

        /// <summary>Restores neutral input after a shot temporarily used the character's facing.</summary>
        public void RestoreShotAim()
        {
            if (m_restoreUnaimed && player) player.WeaponsSystem?.StopAim();
            m_restoreUnaimed = false;
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || player.WeaponsSystem == null) return;
            if (context.performed && !firing)
            {
                firing = true;
                player.WeaponsSystem.BeginFire();
            }
            if (firing && (context.canceled || (context.started && !context.ReadValueAsButton())))
            {
                firing = false;
                if (Application.isFocused && isActiveAndEnabled)
                    player.WeaponsSystem.EndFire();
                else
                    CancelInput();
            }
        }

        public void OnReload(InputAction.CallbackContext context)
        {
            if (!context.performed || inputInterrupted || !player || player.WeaponsSystem == null) return;
            player.WeaponsSystem.Reload();
        }

        public void OnAbility(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !ability) return;
            if (context.performed && !usingAbility)
            {
                usingAbility = true;
                ability.BeginUse();
            }
            if (usingAbility && (context.canceled || (context.started && !context.ReadValueAsButton())))
            {
                usingAbility = false;
                if (Application.isFocused && isActiveAndEnabled)
                    ability.EndUse();
                else
                    ability.CancelUse();
            }
        }

        private Vector3 ToWorld(Vector2 input) => player.Movement?.ToWorld(input) ?? input;

        private void CancelInput()
        {
            m_mouse = null;
            m_aimAction = null;
            m_restoreUnaimed = false;
            firing = false;
            usingAbility = false;
            if (ability) ability.CancelUse();
            if (!player) return;
            if (player.Movement != null) player.Movement.Move(Vector2.zero);
            if (player.WeaponsSystem != null)
            {
                player.WeaponsSystem.CancelInput();
                player.WeaponsSystem.StopAim();
            }
        }

        private void OnEnable() => inputInterrupted = false;

        private void OnDisable()
        {
            inputInterrupted = true;
            CancelInput();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            inputInterrupted = !hasFocus;
            if (!hasFocus) CancelInput();
        }

        private void OnApplicationPause(bool paused)
        {
            inputInterrupted = paused;
            if (paused) CancelInput();
        }
    }

}
