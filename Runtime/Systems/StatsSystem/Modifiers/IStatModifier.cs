namespace RogueLikeEngine.Systems.Stats
{
    public interface IStatModifier
    {
        /// <summary>
        /// The exact value contributed to the stat by this modifier
        /// </summary>
        float Value { get; }

        /// <summary>
        /// Target stat that this modifier will affect
        /// </summary>
        StatDefinition TargetStat { get; }

        /// <summary>
        /// Type of targeting, Entity or Global, when global is picked, a global stat store must be provided
        /// </summary>
        StatModifierTargetType TargetType { get; }

        /// <summary>
        /// Defines how the modifier will affect the stat, flat value, rate increase, force cap, etc.
        /// </summary>
        StatModifierMode Mode { get; }

        /// <summary>
        /// Defines if the modifier affects the base value permanently or not
        /// </summary>
        StatPermanence Permanence { get; }

        /// <summary>
        /// Global stats store, only used when the target type is global
        /// </summary>
        GlobalStatsStore GlobalStatsStore { get; }

        /// <summary>
        /// Is the modifier active or not, when it's not active, it will not affect the stat
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Specifics the order in which the modifiers will be applied to the stat
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Original flat value of the modifier, before any changes
        /// </summary>
        float ValuePreview { get; }

        ModifierTextFormatingOptions FormatingOptions { get; }

    /// <summary>
        /// Called only once when the modifier is added to the stat
        /// </summary>
        /// <param name="statsStore">a reference to the stat store owning the stat</param>
        /// <param name="ownerStat">a reference to the stat owning the modifier</param>
        void Configure(StatsStore statsStore, Stat ownerStat);
    }
}