using System;
using UnityEngine;

namespace RogueLikeEngine.Utils.Timers
{
    [Serializable]
    public struct FloatTimer
    {
        public float timerDuration;
        public float timeLeft;

        public bool IsFinished => timeLeft <= 0;
        public float Percentage => Mathf.Clamp(timeLeft / timerDuration, 0, 1);

        public static implicit operator FloatTimer(float duration)
        {
            return new FloatTimer {timerDuration = duration};
        }
    }

    public static class FloatTimerExtensions
    {
        public static void Reset(this ref FloatTimer timer)
        {
            timer.timeLeft = timer.timerDuration;
        }

        public static void Decrement(this ref FloatTimer timer,float decrement)
        {
            timer.timeLeft -= decrement;
        }
    
        public static void ForceFinish(this ref FloatTimer timer)
        {
            timer.timeLeft = 0;
        }
    }
}