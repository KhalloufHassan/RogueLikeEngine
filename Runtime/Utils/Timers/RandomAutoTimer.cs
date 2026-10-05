using System;
using RogueLikeEngine.Utils.Randomizers;
using UnityEngine;

namespace RogueLikeEngine.Utils.Timers
{
    [Serializable]
    public struct RandomAutoTimer
    {
        public RandomFloatRange randomRange;
        public float resetTimeStamp;
        public float timerDuration;

        public float TimeDiff => Time.timeSinceLevelLoad - resetTimeStamp;
        public float TimeLeft => timerDuration - TimeDiff;
        public bool IsFinished => TimeDiff >= timerDuration;
    }

    public static class RandomAutoTimerExtensions
    {
        public static void Reset(this ref RandomAutoTimer timer)
        {
            timer.resetTimeStamp = Time.timeSinceLevelLoad;
            timer.timerDuration = timer.randomRange.GetRandom();
        }
    }
}
