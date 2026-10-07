using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>
    /// The physics body a movement drives, hiding whether it's 2D (XY plane) or 3D (XZ plane, Y up).
    /// Movements only move through it, so each movement is written once, in world space, and works in both.
    /// </summary>
    public interface IBody
    {
        Vector3 Position { get; }
        Vector3 Velocity { get; }

        /// <summary>Sets the velocity on the movement plane only. In 3D the vertical velocity (gravity, jumps) is kept.</summary>
        void SetPlanarVelocity(Vector3 velocity);

        /// <summary>Sets the full velocity, e.g. for dashes, knockbacks or projectiles.</summary>
        void SetVelocity(Vector3 velocity);

        /// <summary>The direction projected on the movement plane.</summary>
        Vector3 Flatten(Vector3 direction);

        /// <summary>Planar input (stick, keys) to a world direction: XY in 2D, XZ in 3D.</summary>
        Vector3 ToWorld(Vector2 planar);

        /// <summary>Rotation for a character facing the direction: 2D sprites face up, 3D models face forward.</summary>
        Quaternion FacingRotation(Vector3 direction);
    }
}
