using System;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Tries children in order until one runs or succeeds.</summary>
    [Serializable]
    public sealed class BrainSelector : BrainComposite
    {
        protected override BrainStatus OnTick(BrainContext context)
        {
            foreach (BrainNode child in Children)
            {
                BrainStatus status = child.Tick(context);
                if (status != BrainStatus.Failure) return status;
            }
            return BrainStatus.Failure;
        }
    }
}
