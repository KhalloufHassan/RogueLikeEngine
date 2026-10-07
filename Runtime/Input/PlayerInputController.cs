using RogueLikeEngine.Systems.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Entity player;

        private bool firing;
        private bool inputInterrupted;
        private void Update()
        {
            if(firing && !inputInterrupted && player && player.WeaponsSystem != null)
                player.WeaponsSystem.Fire();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || player.Movement == null) return;
            Vector2 movementInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
            player.Movement.Move(ToWorld(movementInput));
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

        private Vector3 ToWorld(Vector2 input) => player.Movement?.ToWorld(input) ?? input;

        private void CancelInput()
        {
            firing = false;
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
