using RogueLikeEngine.Systems.Movements;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public sealed class RecordingMovement : Movement
    {
        public Vector3 LastInput { get; private set; }
        public bool InCover;
        public bool Exposed;
        public override bool IsInCover => InCover;
        public override bool IsExposed => Exposed;

        public override void Move(Vector3 target) => LastInput = target;
    }
}
