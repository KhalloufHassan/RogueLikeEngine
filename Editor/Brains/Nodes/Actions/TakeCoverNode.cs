#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", "", "TakeCover")]
    public sealed class TakeCoverNode : BrainGraphNode
    {

        public override BrainNode Build(BrainGraphCompiler compiler) => new TakeCover();
    }
}
#endif
