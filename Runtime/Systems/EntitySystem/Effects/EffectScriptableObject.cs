using RogueLikeEngine.Utils.Timers;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities.Effects
{
    public class EffectScriptableObject : ScriptableObject,IEffect
    {
        [SerializeField] private string effectName;
        [SerializeField] private Sprite icon;
        [SerializeField] protected AutoTimer duration = float.PositiveInfinity;
    
        public int ID => Name.GetHashCode();
        public string Name => effectName;
        public Sprite Icon => icon;
        public AutoTimer DurationTimer => duration;
        public Entity DamageOwner { get; set; }

        public IEffect GetCopy() => Instantiate(this);

        public virtual void Init(Entity entity)
        {
            duration.Reset();
            DamageOwner = entity;
        }
        
        public virtual void OnStacked(Entity entity) => duration.Reset();
    }
}