using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    public class Stat
    {
        private StatsStore StatsStore { get; }
    
        public StatDefinition StatDefinition { get; }

        public float PermanentValue { get; private set; }
        public float TemporaryValue { get; private set; }
        public float IncreasePercentage { get; set; } = 1.0f;
        public float? Cap { get; private set; }
        public List<StatLink> StatLinks { get; private set; }

        public int FinalValue => Mathf.FloorToInt(FinalFloatValue);

        /// <summary>Returns the final stat value without rounding away fractional precision, limited by the cap if any.</summary>
        public float FinalFloatValue => Mathf.Min(PermanentValue + TemporaryValue, Cap ?? float.PositiveInfinity);

        private readonly IList<IStatModifier> m_modifiers = new List<IStatModifier>();

        public event Action OnValueChanged;

        public Stat(StatDefinition statDefinition,StatsStore store)
        {
            StatsStore = store;
            StatDefinition = statDefinition;
        }

        internal void AddModifier(IStatModifier modifier)
        {
            if(m_modifiers.Contains(modifier)) return;
            m_modifiers.Add(modifier);
            modifier.Configure(StatsStore, this);
            RecalculateValue();
        }
        
        internal void RemoveModifier(IStatModifier modifier)
        {
            if (!m_modifiers.Remove(modifier)) return;
            modifier.Unconfigure();
            RecalculateValue();
        }

        /// <summary>
        /// Recalculates the stat from its active modifiers:
        /// flat values are summed, then scaled by the sum of all rate increases, then limited by the lowest cap.
        /// The result doesn't depend on the order the modifiers were added in.
        /// </summary>
        public void RecalculateValue(bool statLinkUpdate = false)
        {
            float permanentValue = 0;
            float temporaryValue = 0;
            float increasePercentage = 1.0f;
            float? cap = null;
            foreach (IStatModifier statModifier in m_modifiers.OrderByDescending(m => m.Priority).Where(m => m.IsActive))
            {
                switch (statModifier.Mode)
                {
                    case StatModifierMode.FlatValue when statModifier.Permanence == StatPermanence.Temporary:
                        temporaryValue += statModifier.Value;
                        break;
                    case StatModifierMode.FlatValue:
                        permanentValue += statModifier.Value;
                        break;
                    case StatModifierMode.RateIncrease:
                        increasePercentage += statModifier.Value;
                        break;
                    case StatModifierMode.ForceCap:
                        cap = cap.HasValue ? Mathf.Min(cap.Value, statModifier.Value) : statModifier.Value;
                        break;
                }
            }

            IncreasePercentage = increasePercentage;
            PermanentValue = permanentValue * increasePercentage;
            TemporaryValue = temporaryValue * increasePercentage;
            Cap = cap;
            OnValueChanged?.Invoke();
            
            if(!statLinkUpdate && StatLinks != null)
                foreach (StatLink statLink in StatLinks)
                {
                    statLink.DependentStat.RecalculateValue(true);
                }
        }
        
        /// <summary>
        /// Returns the value the given modifier would contribute if it was added to this stat's store.
        /// Read-only: neither this stat, its modifiers nor the new modifier are changed.
        /// </summary>
        public float PreviewModifierValue(IStatModifier newModifier) => newModifier.PreviewValueFor(StatsStore);


        public void AddStatLink(StatLink statLink) => (StatLinks ??= new List<StatLink>()).Add(statLink);
        
        public StatLink GetStatLinkFor(Stat stat) => StatLinks?.FirstOrDefault(sl => sl.DependentStat == stat);
        
        public override string ToString()
        {
            string modifiers = string.Join(",\n", m_modifiers);
            return
                $"Name: {StatDefinition.statName}, BaseValue: {PermanentValue}, IncreasePercentage: {IncreasePercentage}, Cap: {Cap}, FinalValue: {FinalValue}\n Modifiers: {modifiers}";
        }
    }
}
