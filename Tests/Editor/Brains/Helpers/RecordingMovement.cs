using RogueLikeEngine.Systems.Movements;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public sealed class RecordingMovement : Movement
    {
        public Vector3 LastInput { get; private set; }

        public override void Move(Vector3 target) => LastInput = target;
    }
}
