namespace RogueLikeEngine.Systems.Stats
{
    public class StatLink
    {
        public Stat DependentStat { get; }
        public float BaseValueIncreased { get; set; }

        public StatLink(Stat dependentStat,float baseValueIncrease)
        {
            DependentStat = dependentStat;
            BaseValueIncreased = baseValueIncrease;
        }
    }
}