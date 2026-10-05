using System;
using System.Collections.Generic;
using RogueLikeEngine.Systems.Entities.Effects;
using RogueLikeEngine.Systems.Entities.Items;
using RogueLikeEngine.Systems.Healths;
using RogueLikeEngine.Systems.Movements;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Systems.Weapons;
using RogueLikeEngine.Utils;
using UnityEngine;
using UnityEngine.Pool;

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

        /// <summary>True once the starting effects have been applied at least once (e.g. after Start).</summary>
        protected bool StartingEffectsApplied { get; private set; }

        private void Start()
        {
            ApplyStartingEffect();
        }

        /// <summary>
        /// Applies the configured starting effects. The array is kept intact so the effects can be re-applied,
        /// e.g. when a pooled entity is reused, effects already present are stacked instead of duplicated.
        /// </summary>
        protected void ApplyStartingEffect()
        {
            if (m_startingEffects != null)
            {
                foreach (EffectScriptableObject effect in m_startingEffects)
                {
                    if (effect) AddEffect(effect);
                }
            }

            StartingEffectsApplied = true;
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
                if (damageOwnerOverride) copy.DamageOwner = damageOwnerOverride;
                OnEffectAdded?.Invoke(copy);
            }
        }

        /// <summary>
        /// Removes the entity's copy of the given effect (the template or the copy itself can be passed).
        /// Does nothing if the effect is not applied.
        /// </summary>
        public void RemoveEffect(IEffect effect, bool triggerDurationEnded)
        {
            if (!Effects.TryGetValue(effect.ID, out IEffect internalCopy)) return;

            // Remove first so the callback can safely add/remove effects
            Effects.Remove(effect.ID);
            if (triggerDurationEnded && internalCopy is IOnDurationEnded onDurationEnded)
                onDurationEnded.OnDurationEnded(this);
            DestroyCopy(internalCopy);
        }

        /// <summary>Removes every applied effect.</summary>
        public void ClearEffects(bool triggerDurationEnded)
        {
            if (Effects.Count == 0) return;
            using (ListPool<IEffect>.Get(out List<IEffect> snapshot))
            {
                snapshot.AddRange(Effects.Values);
                foreach (IEffect effect in snapshot)
                    RemoveEffect(effect, triggerDurationEnded);
            }
        }

        protected virtual void Update()
        {
            if (Effects.Count == 0) return;
            // Iterate a snapshot: effect callbacks may add or remove effects
            using (ListPool<IEffect>.Get(out List<IEffect> snapshot))
            {
                snapshot.AddRange(Effects.Values);
                foreach (IEffect effect in snapshot)
                {
                    if (!IsApplied(effect)) continue;
                    if (effect is IOnUpdate onUpdate)
                        onUpdate.OnUpdate(this);
                    if (IsApplied(effect) && effect.DurationTimer.IsFinished)
                        RemoveEffect(effect, true);
                }
            }
        }

        private void FixedUpdate()
        {
            if (Effects.Count == 0) return;
            using (ListPool<IEffect>.Get(out List<IEffect> snapshot))
            {
                snapshot.AddRange(Effects.Values);
                foreach (IEffect effect in snapshot)
                {
                    if (IsApplied(effect) && effect is IOnFixedUpdate onFixedUpdate)
                        onFixedUpdate.OnFixedUpdate(this);
                }
            }
        }

        protected virtual void OnCollisionEnter2D(Collision2D other)
        {
            if (Effects.Count == 0) return;
            using (ListPool<IEffect>.Get(out List<IEffect> snapshot))
            {
                snapshot.AddRange(Effects.Values);
                foreach (IEffect effect in snapshot)
                {
                    if (IsApplied(effect) && effect is IOnHit onHit)
                        onHit.OnHit(this, other);
                }
            }
        }

        private static void DestroyCopy(object copy)
        {
            if (copy is UnityEngine.Object unityCopy) UnityObjectUtility.DestroySafely(unityCopy);
        }

        private bool IsApplied(IEffect effect) => Effects.TryGetValue(effect.ID, out IEffect current) && ReferenceEquals(current, effect);

        public void AllSystemsActive(bool isActive)
        {
            if (Movement) Movement.IsSystemActive = isActive;
            if (Health) Health.IsSystemActive = isActive;
            if (WeaponsSystem) WeaponsSystem.IsSystemActive = isActive;
        }



        public virtual void DestroyEntity()
        {
            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            ClearEffects(false);
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
