using System.Collections.Generic;

namespace RogueLikeEngine.Systems.Stats
{
    public class StatsStore
    {
        private readonly IDictionary<StatDefinition, Stat> m_stats = new Dictionary<StatDefinition, Stat>();

        public Stat GetOrCreateStat(StatDefinition statDefinition) => m_stats.TryGetValue(statDefinition, out Stat stat) ? stat : CreateStat(statDefinition);
        
        public void AddModifier(IStatModifier modifier) => GetOrCreateStat(modifier.TargetStat).AddModifier(modifier);
        
        public void RemoveModifier(IStatModifier modifier) => GetOrCreateStat(modifier.TargetStat).RemoveModifier(modifier);
        public void ReCalculate(IStatModifier modifier) => GetOrCreateStat(modifier.TargetStat).RecalculateValue();

        private Stat CreateStat(StatDefinition statDefinition) => m_stats[statDefinition] = new Stat(statDefinition,this);

    }
}