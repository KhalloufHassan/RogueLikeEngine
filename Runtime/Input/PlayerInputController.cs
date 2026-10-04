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
            if(firing && !inputInterrupted && player && player.WeaponsSystem)
                player.WeaponsSystem.Fire();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || !player.Movement) return;
            Vector2 movementInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
            player.Movement.Move(movementInput);
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || !player.WeaponsSystem) return;
            if (context.canceled) 
                player.WeaponsSystem.StopAim();
            else
                player.WeaponsSystem.Aim(context.ReadValue<Vector2>());
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (inputInterrupted || !player || !player.WeaponsSystem) return;
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

        public void CancelInput()
        {
            firing = false;
            if (!player) return;
            if (player.Movement) player.Movement.Move(Vector2.zero);
            if (player.WeaponsSystem)
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
