using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueLikeEngine.Input
{
    public class MousePositionToAimProcessor : InputProcessor<Vector2>
    {
        private GameObject m_player;
        private Camera m_camera;
        
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (!m_player) m_player = GameObject.FindWithTag("Player");
            if(!m_camera) m_camera = Camera.main;
            
            if(!m_camera || !m_player) return value;

            Vector3 mouseWorldPos = m_camera.ScreenToWorldPoint(new Vector3(value.x, value.y, -m_camera.transform.position.z));
            Vector3 objectPos = m_player.transform.position;

            Vector2 direction = (mouseWorldPos - objectPos).normalized;
            return direction;
        }
    }
}