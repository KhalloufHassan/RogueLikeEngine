using RogueLikeEngine.Systems.Weapons;

namespace RogueLikeEngine.Tests
{
    public sealed class RecordingWeaponsSystem : WeaponsSystem
    {
        public int Shots { get; private set; }
        public override bool CanFire => IsSystemActive;
        public override void Fire() => Shots++;
    }
}
