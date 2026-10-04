using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Entities.Effects;

namespace RogueLikeEngine.Tests
{
    /// <summary>Effect that records its callbacks and can add or remove other effects while updating.</summary>
    public class TestEffect : EffectScriptableObject, IOnUpdate, IOnDurationEnded
    {
        public int updateCount;
        public int durationEndedCount;
        public TestEffect removeOnUpdate;
        public TestEffect addOnUpdate;

        internal static TestEffect Create(TestObjects objects, string effectName, float durationSeconds = float.PositiveInfinity)
        {
            TestEffect effect = objects.CreateAsset<TestEffect>();
            TestReflection.SetField(effect, "effectName", effectName);
            effect.duration = durationSeconds;
            return effect;
        }

        public void OnUpdate(Entity entity)
        {
            updateCount++;
            if (removeOnUpdate) entity.RemoveEffect(removeOnUpdate, false);
            if (addOnUpdate) entity.AddEffect(addOnUpdate);
        }

        public void OnDurationEnded(Entity entity) => durationEndedCount++;
    }
}
