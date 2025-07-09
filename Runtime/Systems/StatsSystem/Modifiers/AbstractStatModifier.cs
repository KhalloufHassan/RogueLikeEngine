using System;
using RogueLikeEngine.Attributes;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [Serializable]
    public abstract class AbstractStatModifier : IStatModifier
    {
        [SerializeField] protected StatModifierTargetType targetType;
        [SerializeField] protected StatModifierMode mode;
        [SerializeField] protected StatPermanence statPermanence;
        [SerializeField] protected StatDefinition targetStat;
        [SerializeField] protected ModifierTextFormatingOptions formatingOptions;
        [ShowIf(nameof(targetType), StatModifierTargetType.Global),SerializeField] protected GlobalStatsStore globalStatsStore;

        public StatDefinition TargetStat => targetStat;
        public StatModifierTargetType TargetType => targetType;
        public StatModifierMode Mode => mode;
        public StatPermanence Permanence => statPermanence;
        public GlobalStatsStore GlobalStatsStore => globalStatsStore;
        public bool IsActive { get; set; } = true;
        public virtual int Priority => 0;


        public abstract float Value { get; }
        public abstract float ValuePreview { get; }
        public ModifierTextFormatingOptions FormatingOptions => formatingOptions;
        
        
        public StatsStore StatsStore { get; set; }
        public Stat OwnerStat { get; set; }
        
        public virtual void Configure(StatsStore statsStore, Stat ownerStat)
        {
            StatsStore = statsStore;
            OwnerStat = ownerStat;
        }

    }
}