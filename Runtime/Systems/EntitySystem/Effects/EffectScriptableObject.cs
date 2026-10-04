using System;
using RogueLikeEngine.Utils.Timers;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities.Effects
{
    public class EffectScriptableObject : ScriptableObject,IEffect
    {
        [SerializeField] private string effectName;
        [SerializeField] private Sprite icon;
        [SerializeField] protected AutoTimer duration = float.PositiveInfinity;

        private static int s_nextId;

        /// <summary>Not serialized, so Instantiate doesn't copy it, GetCopy assigns it explicitly.</summary>
        [NonSerialized] private int m_id;

        /// <summary>Unique per template asset for the session, shared by all copies made from it.</summary>
        public int ID => m_id != 0 ? m_id : m_id = ++s_nextId;
        public string Name => effectName;
        public Sprite Icon => icon;
        public AutoTimer DurationTimer => duration;
        public Entity DamageOwner { get; set; }

        public IEffect GetCopy()
        {
            EffectScriptableObject copy = Instantiate(this);
            copy.m_id = ID;
            return copy;
        }

        public virtual void Init(Entity entity)
        {
            duration.Reset();
            DamageOwner = entity;
        }
        
        public virtual void OnStacked(Entity entity) => duration.Reset();
    }
}