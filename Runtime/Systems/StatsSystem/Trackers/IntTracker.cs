using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [CreateAssetMenu(menuName = "Trackers/" + nameof(IntTracker),fileName = nameof(IntTracker))]
    public class IntTracker : Tracker<int>
    {
        
    }
}