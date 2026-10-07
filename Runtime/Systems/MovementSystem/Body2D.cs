using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>2D body: moves on the XY plane.</summary>
    public class Body2D : IBody
    {
        private readonly Rigidbody2D m_rigidbody;

        public Body2D(Rigidbody2D rigidbody)
        {
            m_rigidbody = rigidbody;
        }

        public Vector3 Position => m_rigidbody.position;
        public Vector3 Velocity => m_rigidbody.linearVelocity;

        public void SetPlanarVelocity(Vector3 velocity) => m_rigidbody.linearVelocity = velocity;
        public void SetVelocity(Vector3 velocity) => m_rigidbody.linearVelocity = velocity;

        public Vector3 Flatten(Vector3 direction) => new(direction.x, direction.y, 0);
        public Vector3 ToWorld(Vector2 planar) => planar;

        public Quaternion FacingRotation(Vector3 direction) => Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }
}
