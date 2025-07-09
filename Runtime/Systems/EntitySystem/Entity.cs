using System;
using System.Collections.Generic;
using RogueLikeEngine.Extensions;
using RogueLikeEngine.Systems.Entities.Effects;
using RogueLikeEngine.Systems.Entities.Items;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using UnityEngine;

namespace RogueLikeEngine.Systems.Entities
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] private Movement m_movement;
        [SerializeField] private WeaponsSystem m_weaponsSystem;
        [SerializeField] private Health m_health;
        [SerializeField] private EffectScriptableObject[] m_startingEffects;

        public StatsStore StatsStore { get; private set; } = new();

        public Movement Movement => m_movement;
        public WeaponsSystem WeaponsSystem => m_weaponsSystem;
        public Health Health => m_health;

        public event Action<IEffect> OnEffectAdded;

        private IDictionary<int, IEffect> Effects { get; set; } = new Dictionary<int, IEffect>();
        private readonly HashSet<int> m_markedForDeletion = new();

        private void Start()
        {
            ApplyStartingEffect();
        }

        private void ApplyStartingEffect()
        {
            foreach (EffectScriptableObject effect in m_startingEffects)
            {
                AddEffect(effect);
            }

            m_startingEffects.Clear();
        }

        public void AddEffect(IEffect effect, Entity damageOwnerOverride = null)
        {
            if (Effects.TryGetValue(effect.ID, out IEffect internalCopy))
            {
                internalCopy.OnStacked(this);
                OnEffectAdded?.Invoke(internalCopy);
            }
            else
            {
                IEffect copy = effect.GetCopy();
                Effects[copy.ID] = copy;
                copy.Init(this);
                OnEffectAdded?.Invoke(copy);
            }
        }

        public void RemoveEffect(IEffect effect, bool triggerDurationEnded)
        {
            int effectID = effect.ID;
            IEffect e = Effects[effectID];
            if (e != null)
            {
                if (triggerDurationEnded && effect is IOnDurationEnded onDurationEnded)
                    onDurationEnded.OnDurationEnded(this);
                Effects.Remove(effectID);
            }
        }

        protected virtual void Update()
        {
            if (Effects.Count == 0) return;
            foreach (IEffect effect in Effects.Values)
            {
                if (effect is IOnUpdate onUpdate)
                    onUpdate.OnUpdate(this);
                if (effect.DurationTimer.IsFinished)
                {
                    if (effect is IOnDurationEnded onDurationEnded)
                        onDurationEnded.OnDurationEnded(this);
                    m_markedForDeletion.Add(effect.ID);
                }
            }

            foreach (int id in m_markedForDeletion)
            {
                Effects.Remove(id);
            }

            m_markedForDeletion.Clear();
        }

        private void FixedUpdate()
        {
            foreach (IEffect effect in Effects.Values)
            {
                if (effect is IOnFixedUpdate onFixedUpdate)
                    onFixedUpdate.OnFixedUpdate(this);
            }
        }

        protected virtual void OnCollisionEnter2D(Collision2D other)
        {
            foreach (IEffect effect in Effects.Values)
            {
                if (effect is IOnHit onHit)
                    onHit.OnHit(this, other);
            }
        }

        public void AllSystemsActive(bool isActive)
        {
            if (Movement) Movement.IsSystemActive = isActive;
            if (Health) Health.IsSystemActive = isActive;
            if (WeaponsSystem) WeaponsSystem.IsSystemActive = isActive;
        }



        public void DestroyEntity()
        {
            Destroy(gameObject);
        }

        //TODO this should be in a manager
        public void AddItem(IItem item)
        {
            item = item.GetCopy();
            foreach (IStatModifier modifier in item.Modifiers)
            {
                if (modifier.TargetType == StatModifierTargetType.Entity)
                    StatsStore.AddModifier(modifier);
                else
                {
                    modifier.GlobalStatsStore.Store.AddModifier(modifier);
                }
            }
        }

    }
}
