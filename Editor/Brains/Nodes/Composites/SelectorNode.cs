#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Flow", "", "Selector")]
    public sealed class SelectorNode : CompositeNode
    {
        public override BrainNode Build(BrainGraphCompiler compiler) => new BrainSelector { Children = compiler.Children(this) };
    }
}
#endif
