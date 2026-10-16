using System;
using RogueLikeEngine.Systems.Brains;

namespace RogueLikeEngine.Tests
{
    [Serializable]
    internal sealed class Probe : BrainNode
    {
        public BrainStatus Result = BrainStatus.Running;
        public int Starts;
        public int Ticks;
        public int Stops;
        [NonSerialized] public Action OnTickCallback;

        protected override void OnStart(BrainContext context) => Starts++;

        protected override BrainStatus OnTick(BrainContext context)
        {
            Ticks++;
            OnTickCallback?.Invoke();
            return Result;
        }

        protected override void OnStop(BrainContext context) => Stops++;
    }
}
