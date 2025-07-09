using RogueLikeEngine.Systems.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private Entity player;

        private bool firing;
        private void Update()
        {
            if(firing)
                player.WeaponsSystem.Fire();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 movementInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
            player.Movement.Move(movementInput);
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.canceled) 
                player.WeaponsSystem.StopAim();
            else
                player.WeaponsSystem.Aim(context.ReadValue<Vector2>());
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if(context.started) firing = true;
            if (context.canceled) firing = false;
        }
    }

}