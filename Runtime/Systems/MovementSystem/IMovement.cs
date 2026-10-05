using RogueLikeEngine.Systems.Entities;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    public interface IMovement : IEntitySystem
    {
        Vector2 MovementDirection { get; }
        bool IsMoving { get; }
        float TraveledDistance { get; }
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        float Speed { get; }

        void Move(Vector2 direction);
        void SetExternalVelocity(Vector2 velocity, float deltaTime);
        void ClearExternalVelocity();
        void ResetTraveledDistance();
    }
}
