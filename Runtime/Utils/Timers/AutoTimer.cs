using System;
using UnityEngine;

namespace RogueLikeEngine.Utils.Timers
{
    [Serializable]
    public struct AutoTimer
    {
        public float timerDuration;
        public float resetTimeStamp;

        public float Percentage => Mathf.Clamp(TimeLeft / timerDuration, 0, 1);
        public float TimeDiff => Time.timeSinceLevelLoad - resetTimeStamp;
        public float TimeLeft => timerDuration - TimeDiff;

        public bool IsFinished => TimeDiff >= timerDuration;

        public static implicit operator AutoTimer(float duration)
        {
            return new AutoTimer {timerDuration = duration};
        }
    }

    public static class AutoTimerExtensions
    {
        public static void Reset(this ref AutoTimer timer)
        {
            timer.resetTimeStamp = Time.timeSinceLevelLoad;
        }
    }
}
