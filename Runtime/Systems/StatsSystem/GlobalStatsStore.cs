using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [CreateAssetMenu(fileName = nameof(GlobalStatsStore),menuName = "Stats/" + nameof(GlobalStatsStore))]
    public class GlobalStatsStore : ScriptableObject
    {
        public StatsStore Store { get; } = new();
    }
}