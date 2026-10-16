using System;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Ticks unfinished children together. Any failure interrupts the others; all must succeed.</summary>
    [Serializable]
    public sealed class BrainParallel : BrainComposite
    {
        protected override BrainStatus OnTick(BrainContext context)
        {
            bool running = false;
            foreach (BrainNode child in Children)
            {
                BrainStatus status = child.Tick(context);
                if (status == BrainStatus.Failure) return status;
                running |= status == BrainStatus.Running;
            }
            return running ? BrainStatus.Running : BrainStatus.Success;
        }
    }
}
