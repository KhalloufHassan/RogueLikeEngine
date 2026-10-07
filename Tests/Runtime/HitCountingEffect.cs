using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Entities.Effects;

namespace RogueLikeEngine.Tests.PlayMode
{
    /// <summary>Counts the hits of the entity it's applied to.</summary>
    public class HitCountingEffect : EffectScriptableObject, IOnHit
    {
        public int hits;
        public HitInfo lastHit;

        public void OnHit(Entity entity, HitInfo hit)
        {
            hits++;
            lastHit = hit;
        }
    }
}
