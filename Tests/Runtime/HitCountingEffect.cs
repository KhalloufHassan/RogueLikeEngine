using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Entities.Effects;
using UnityEngine;

namespace RogueLikeEngine.Tests.PlayMode
{
    /// <summary>Counts the hits of the entity it's applied to.</summary>
    public class HitCountingEffect : EffectScriptableObject, IOnHit
    {
        public int hits;

        public void OnHit(Entity entity, Collision2D collision) => hits++;
    }
}
