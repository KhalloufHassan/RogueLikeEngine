#if UNITY_6000_6_OR_NEWER
using System;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using Unity.GraphToolkit.Editor;

namespace RogueLikeEngine.EditorTools.Brains
{
    [Serializable, Node("Brain/Actions", "", "Fire At Target")]
    public sealed class FireAtTargetNode : BrainGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            context.AddInputPort<Entity>("Target").Build();
        }

        public override BrainNode Build(BrainGraphCompiler compiler) => new FireAtTarget
        {
            Target = compiler.Value<Entity>(GetInputPortByName("Target"))
        };
    }
}
#endif
