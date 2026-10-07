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

        public IMovement Movement => m_movement ? m_movement : null;
        public IWeaponsSystem WeaponsSystem => m_weaponsSystem ? m_weaponsSystem : null;
        public IHealth Health => m_health ? m_health : null;

        public event Action<IEffect> OnEffectAdded;

        private IDictionary<int, IEffect> Effects { get; set; } = new Dictionary<int, IEffect>();
        private readonly List<IOnUpdate> m_updateEffects = new();
        private readonly List<IOnFixedUpdate> m_fixedUpdateEffects = new();
        private readonly List<IOnHit> m_hitEffects = new();

        /// <summary>True once the starting effects have been applied at least once (e.g. after Start).</summary>
        private bool StartingEffectsApplied { get; set; }

        private void Start()
        {
            if (!StartingEffectsApplied) ApplyStartingEffect();
        }

        /// <summary>
        /// Applies the configured starting effects. The array is kept intact so the effects can be re-applied,
        /// e.g. when a pooled entity is reused, effects already present are stacked instead of duplicated.
        /// </summary>
        public void ApplyStartingEffect()
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
                RegisterHooks(copy);
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
            UnregisterHooks(internalCopy);
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
            using (ListPool<IOnUpdate>.Get(out List<IOnUpdate> updates))
            {
                updates.AddRange(m_updateEffects);
                foreach (IOnUpdate effect in updates)
                {
                    if (m_updateEffects.Contains(effect)) effect.OnUpdate(this);
                }
            }

            using (ListPool<IEffect>.Get(out List<IEffect> snapshot))
            {
                snapshot.AddRange(Effects.Values);
                foreach (IEffect effect in snapshot)
                {
                    if (IsApplied(effect) && effect.DurationTimer.IsFinished)
                        RemoveEffect(effect, true);
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_fixedUpdateEffects.Count == 0) return;
            using (ListPool<IOnFixedUpdate>.Get(out List<IOnFixedUpdate> fixedUpdates))
            {
                fixedUpdates.AddRange(m_fixedUpdateEffects);
                foreach (IOnFixedUpdate effect in fixedUpdates)
                {
                    if (m_fixedUpdateEffects.Contains(effect)) effect.OnFixedUpdate(this);
                }
            }
        }

        private void OnCollisionEnter(Collision collision) => HandleHit(HitInfo.From(collision));
        private void OnCollisionEnter2D(Collision2D collision) => HandleHit(HitInfo.From(collision));
        private void OnTriggerEnter(Collider other) => HandleHit(HitInfo.From(other, transform.position));
        private void OnTriggerEnter2D(Collider2D other) => HandleHit(HitInfo.From(other, transform.position));

        protected virtual void HandleHit(HitInfo hit)
        {
            if (m_hitEffects.Count == 0) return;
            using (ListPool<IOnHit>.Get(out List<IOnHit> hits))
            {
                hits.AddRange(m_hitEffects);
                foreach (IOnHit effect in hits)
                {
                    if (m_hitEffects.Contains(effect)) effect.OnHit(this, hit);
                }
            }
        }

        private void RegisterHooks(IEffect effect)
        {
            if (effect is IOnUpdate onUpdate) m_updateEffects.Add(onUpdate);
            if (effect is IOnFixedUpdate onFixedUpdate) m_fixedUpdateEffects.Add(onFixedUpdate);
            if (effect is IOnHit onHit) m_hitEffects.Add(onHit);
        }

        private void UnregisterHooks(IEffect effect)
        {
            if (effect is IOnUpdate onUpdate) m_updateEffects.Remove(onUpdate);
            if (effect is IOnFixedUpdate onFixedUpdate) m_fixedUpdateEffects.Remove(onFixedUpdate);
            if (effect is IOnHit onHit) m_hitEffects.Remove(onHit);
        }

        private static void DestroyCopy(object copy)
        {
            if (copy is UnityEngine.Object unityCopy) UnityObjectUtility.DestroySafely(unityCopy);
        }

        private bool IsApplied(IEffect effect) => Effects.TryGetValue(effect.ID, out IEffect current) && ReferenceEquals(current, effect);

        public void AllSystemsActive(bool isActive)
        {
            if (Movement != null) Movement.IsSystemActive = isActive;
            if (Health != null) Health.IsSystemActive = isActive;
            if (WeaponsSystem != null) WeaponsSystem.IsSystemActive = isActive;
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
