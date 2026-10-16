using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains.Nodes
{
    [Serializable]
    public abstract class BrainComposite : BrainNode
    {
        [SerializeReference] public BrainNode[] Children = Array.Empty<BrainNode>();

        protected override void OnStop(BrainContext context)
        {
            foreach (BrainNode child in Children) child.Reset(context);
        }
    }
}
