using System;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    /// <summary>Runs children in order until one fails or all succeed.</summary>
    [Serializable]
    public sealed class BrainSequence : BrainComposite
    {
        protected override BrainStatus OnTick(BrainContext context)
        {
            foreach (BrainNode child in Children)
            {
                BrainStatus status = child.Tick(context);
                if (status != BrainStatus.Success) return status;
            }
            return BrainStatus.Success;
        }
    }
}
