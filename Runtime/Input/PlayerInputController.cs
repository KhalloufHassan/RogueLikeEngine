using RogueLikeEngine.Systems.Abilities;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Entity player;
        [SerializeField] private Ability ability;

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
            if (inputInterrupted || !player || player.WeaponsSystem == null) return;
            if (context.canceled) 
                player.WeaponsSystem.StopAim();
            else
                player.WeaponsSystem.Aim(ToWorld(context.ReadValue<Vector2>()));
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
