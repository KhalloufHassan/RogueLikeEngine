using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class MousePositionToAimProcessor : InputProcessor<Vector2>
    {
        private GameObject m_player;
        private Camera m_camera;
        private bool m_is3D;
        
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (!m_player)
            {
                m_player = GameObject.FindWithTag("Player");
                m_is3D = m_player && m_player.GetComponentInChildren<Rigidbody>();
            }
            if(!m_camera) m_camera = Camera.main;
            
            if(!m_camera || !m_player) return value;
            if (m_is3D) return AimOnGround(value);

            Vector3 mouseWorldPos = m_camera.ScreenToWorldPoint(new Vector3(value.x, value.y, -m_camera.transform.position.z));
            Vector3 objectPos = m_player.transform.position;

            Vector2 direction = (mouseWorldPos - objectPos).normalized;
            return direction;
        }

        /// <summary>Top-down 3D: where the mouse ray meets the ground plane at the player's height, as an XZ direction.</summary>
        private Vector2 AimOnGround(Vector2 mousePosition)
        {
            Vector3 playerPosition = m_player.transform.position;
            Ray ray = m_camera.ScreenPointToRay(mousePosition);
            if (!new Plane(Vector3.up, playerPosition).Raycast(ray, out float distance)) return Vector2.zero;

            Vector3 direction = ray.GetPoint(distance) - playerPosition;
            return new Vector2(direction.x, direction.z).normalized;
        }
    }
}