using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>3D body: moves on the XZ plane with Y up, keeping its vertical velocity for gravity.</summary>
    public class Body3D : IBody
    {
        private readonly Rigidbody m_rigidbody;

        public Body3D(Rigidbody rigidbody)
        {
            m_rigidbody = rigidbody;
        }

        public Vector3 Position => m_rigidbody.position;
        public Vector3 Velocity => m_rigidbody.linearVelocity;

        public void SetPlanarVelocity(Vector3 velocity)
        {
            velocity.y = m_rigidbody.linearVelocity.y;
            m_rigidbody.linearVelocity = velocity;
        }

        public void SetVelocity(Vector3 velocity) => m_rigidbody.linearVelocity = velocity;

        public Vector3 Flatten(Vector3 direction) => new(direction.x, 0, direction.z);
        public Vector3 ToWorld(Vector2 planar) => new(planar.x, 0, planar.y);

        public Quaternion FacingRotation(Vector3 direction) => Quaternion.LookRotation(Flatten(direction), Vector3.up);
    }
}
