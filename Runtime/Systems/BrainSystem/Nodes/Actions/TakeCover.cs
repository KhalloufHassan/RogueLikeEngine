using System;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    [Serializable]
    public sealed class TakeCover : BrainNode
    {
        protected override BrainStatus OnTick(BrainContext context)
        {
            if (context.Entity.Movement == null) return BrainStatus.Failure;
            context.Entity.Movement.TakeCover();
            return context.Entity.Movement.IsInCover ? BrainStatus.Success : BrainStatus.Failure;
        }
    }
}
