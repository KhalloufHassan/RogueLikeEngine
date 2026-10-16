using System;
using RogueLikeEngine.Systems.Brains;

namespace RogueLikeEngine.Tests
{
    [Serializable]
    internal sealed class CountTicks : BrainNode
    {
        public int Ticks;

        protected override void OnStart(BrainContext context) => Ticks = 0;

        protected override BrainStatus OnTick(BrainContext context)
        {
            context.Set("Count", ++Ticks);
            return BrainStatus.Running;
        }
    }
}
