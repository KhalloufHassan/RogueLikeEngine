using System;
using RogueLikeEngine.Utils.Randomizers;

namespace RogueLikeEngine.Utils.Timers
{
    [Serializable]
    public struct RandomFloatTimer
    {
        public RandomFloatRange randomRange;
        public float timeLeft;

        public bool IsFinished => timeLeft <= 0;
    }

    public static class RandomFloatTimerExtensions
    {
        public static void Reset(this ref RandomFloatTimer timer)
        {
            timer.timeLeft = timer.randomRange.GetRandom();
        }

        public static void Decrement(this ref RandomFloatTimer timer,float decrement)
        {
            timer.timeLeft -= decrement;
        }
    
        public static void ForceFinish(this ref RandomFloatTimer timer)
        {
            timer.timeLeft = 0;
        }
    }
}
