using RogueLikeEngine.Systems.Abilities;

namespace RogueLikeEngine.Tests.PlayMode.Input
{
    public class RecordingAbility : Ability
    {
        public int Begins { get; private set; }
        public int Ends { get; private set; }
        public int Cancels { get; private set; }

        public override void BeginUse() => Begins++;
        public override void EndUse() => Ends++;
        public override void CancelUse() => Cancels++;

        public void StartTestCooldown() => StartCooldown();
    }
}
