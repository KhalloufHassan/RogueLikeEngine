using RogueLikeEngine.Utils.Timers;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities.Effects
{
    public interface IEffect
    {
        int ID { get; }
        string Name { get; }
        Sprite Icon { get; }
        AutoTimer DurationTimer { get; }
        
        /// <summary>
        /// The only reason we need this, is to support Scriptable Objects as Effects
        /// Because SOs are shared, we can't use their private variables and we need a runtime copy
        /// In any other case, this function is useless and should return the instance itself for normal classes
        /// </summary>
        /// <returns>The copy that will be used for this entity</returns>
        IEffect GetCopy();
        
        /// <summary>
        /// Init is triggered when the effect is first added to the entity
        /// </summary>
        /// <param name="entity">The affected entity</param>
        void Init(Entity entity);
        
        /// <summary>
        /// Called when the effect is added to the entity while it already exists
        /// </summary>
        /// <param name="entity">The affected entity</param>
        void OnStacked(Entity entity);
    }
}