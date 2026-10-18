using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public interface IMovement : IEntitySystem
    {
        Vector3 MovementDirection { get; }
        bool IsMoving { get; }
        bool IsInCover { get; }
        bool IsExposed { get; }
        float TraveledDistance { get; }
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        float Speed { get; }

        /// <summary>Planar input (stick, keys) to a world direction this movement can use.</summary>
        Vector3 ToWorld(Vector2 planar);

        void Move(Vector3 direction);
        void TakeCover();
        void LeaveCover();
        void SetCoverExposure(bool exposed, Vector3 aimDirection);
        void SetExternalVelocity(Vector3 velocity, float deltaTime);
        void ClearExternalVelocity();
        void ResetTraveledDistance();
    }
}
