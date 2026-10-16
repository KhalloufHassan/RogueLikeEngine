#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Flow", "", "Sequence")]
    public sealed class SequenceNode : CompositeNode
    {
        public override BrainNode Build(BrainGraphCompiler compiler) => new BrainSequence { Children = compiler.Children(this) };
    }
}
#endif
