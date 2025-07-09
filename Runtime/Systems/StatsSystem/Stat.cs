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

        public int FinalValue => Mathf.FloorToInt(Mathf.Max(PermanentValue + TemporaryValue,  Cap ?? float.NegativeInfinity));

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
            if (m_modifiers.Remove(modifier))
                RecalculateValue();
        }

        public void RecalculateValue(bool statLinkUpdate = false)
        {
            PermanentValue = 0;
            TemporaryValue = 0;
            Cap = null;
            IncreasePercentage = 1.0f;
            foreach (IStatModifier statModifier in m_modifiers.OrderByDescending(m => m.Priority).Where(m => m.IsActive))
            {
                if(statModifier.Mode == StatModifierMode.FlatValue)
                {
                    if(statModifier.Permanence is StatPermanence.Permanent or StatPermanence.FinalValue)
                        PermanentValue += statModifier.Value * IncreasePercentage;
                    else if(statModifier.Permanence == StatPermanence.Temporary)
                        TemporaryValue +=  statModifier.Value * IncreasePercentage;
                }
                else if(statModifier.Mode == StatModifierMode.RateIncrease)
                    IncreasePercentage += statModifier.Value;
                else if(statModifier.Mode == StatModifierMode.ForceCap)
                    Cap = statModifier.Value;
            }
            OnValueChanged?.Invoke();
            
            if(!statLinkUpdate && StatLinks != null)
                foreach (StatLink statLink in StatLinks)
                {
                    statLink.DependentStat.RecalculateValue(true);
                }
        }
        
        public float PreviewModifierValue(IStatModifier newModifier)
        {
            Stat previewStat = new(StatDefinition, StatsStore);
    
            foreach (IStatModifier modifier in m_modifiers)
            {
                //TODO this is dangerous ? we need a deep copy
                previewStat.AddModifier(modifier);
            }
    
            previewStat.AddModifier(newModifier);
            return newModifier.Value;
        }


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
