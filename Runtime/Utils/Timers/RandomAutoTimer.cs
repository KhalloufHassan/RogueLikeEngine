using System;
using RogueLikeEngine.Utils.Randomizers;

namespace RogueLikeEngine.Utils.Timers
{
    [Serializable]
    public struct RandomAutoTimer
    {
        public RandomFloatRange randomRange;
        public long resetTimeTicks;
        public float timerDuration;
    
        public TimeSpan TimeDiff => DateTime.Now - new DateTime(resetTimeTicks);
        public double TimeLeft => timerDuration - TimeDiff.TotalSeconds;
        public bool IsFinished => TimeDiff.TotalSeconds >= timerDuration;

    }

    public static class RandomAutoTimerExtensions
    {
        public static void Reset(this ref RandomAutoTimer timer)
        {
            timer.resetTimeTicks = DateTime.Now.Ticks;
            timer.timerDuration = timer.randomRange.GetRandom();
        }
    } 
}

